using System;
using GSSystemAnalyzer.Services;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services
{
	public class ThresholdAlertTrackerTests
	{
		private static DateTimeOffset T0 => new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

		// ── Debounce ───────────────────────────────────────────────────

		[Fact]
		public void SingleSample_AboveThreshold_DoesNotFire_WhenDebounceRequired()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 5);

			var result = tracker.Evaluate(isAboveThreshold: true, currentValue: 90.0, threshold: 85.0, T0);

			Assert.Equal(AlertAction.None, result.Action);
			Assert.False(tracker.IsAlerting);
			Assert.Equal(1, tracker.ConsecutiveCount);
		}

		[Fact]
		public void FiveConsecutiveSamples_AboveThreshold_Fires()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 5);
			var time = T0;

			for (int i = 0; i < 4; i++)
			{
				var r = tracker.Evaluate(true, 90.0, 85.0, time);
				Assert.Equal(AlertAction.None, r.Action);
				time = time.AddSeconds(2);
			}

			// 5th consecutive sample should fire
			var result = tracker.Evaluate(true, 90.0, 85.0, time);
			Assert.Equal(AlertAction.Fire, result.Action);
			Assert.Equal(90.0, result.CurrentValue);
			Assert.Equal(T0, result.FirstDetectedAt);
		}

		[Fact]
		public void BrokenRun_ResetsCounter_DoesNotFire()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 5);
			var time = T0;

			// 4 above threshold
			for (int i = 0; i < 4; i++)
			{
				tracker.Evaluate(true, 90.0, 85.0, time);
				time = time.AddSeconds(2);
			}

			Assert.Equal(4, tracker.ConsecutiveCount);

			// 1 below threshold — resets the counter
			tracker.Evaluate(false, 80.0, 85.0, time);
			Assert.Equal(0, tracker.ConsecutiveCount);

			time = time.AddSeconds(2);

			// 4 more above threshold — still not enough
			for (int i = 0; i < 4; i++)
			{
				var r = tracker.Evaluate(true, 90.0, 85.0, time);
				Assert.Equal(AlertAction.None, r.Action);
				time = time.AddSeconds(2);
			}

			Assert.Equal(4, tracker.ConsecutiveCount);
			Assert.False(tracker.IsAlerting);
		}

		[Fact]
		public void SingleConsecutive_FiresImmediately()
		{
			// Disk mode: N=1
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 1);

			var result = tracker.Evaluate(true, 92.0, 90.0, T0);

			Assert.Equal(AlertAction.Fire, result.Action);
			Assert.Equal(92.0, result.CurrentValue);
		}

		// ── Hysteresis ─────────────────────────────────────────────────

		[Fact]
		public void Hysteresis_SmallDip_DoesNotClear()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 1, hysteresisBand: 5.0);

			// Fire
			var fire = tracker.Evaluate(true, 92.0, 90.0, T0);
			Assert.Equal(AlertAction.Fire, fire.Action);
			tracker.RecordAlertFired(92.0, T0);

			// Dip to 87.0% (within hysteresis band: 90 - 5 = 85, and 87 > 85)
			var result = tracker.Evaluate(true, 87.0, 90.0, T0.AddSeconds(10));
			Assert.Equal(AlertAction.None, result.Action);
			Assert.True(tracker.IsAlerting);
		}

		[Fact]
		public void Recovery_BelowHysteresisBand_Clears()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 1, hysteresisBand: 5.0);

			// Fire
			tracker.Evaluate(true, 92.0, 90.0, T0);
			tracker.RecordAlertFired(92.0, T0);

			// Drop to exactly 85.0% (threshold - band = 85.0) → should clear
			var result = tracker.Evaluate(false, 85.0, 90.0, T0.AddSeconds(60));
			Assert.Equal(AlertAction.Clear, result.Action);

			// After recording clear, state resets
			tracker.RecordCleared();
			Assert.False(tracker.IsAlerting);
			Assert.Equal(0, tracker.ConsecutiveCount);
			Assert.Null(tracker.FirstDetectedAt);
		}

		[Fact]
		public void Recovery_ThenReCross_FiresFreshAlert()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 1, hysteresisBand: 5.0);

			// Fire
			tracker.Evaluate(true, 92.0, 90.0, T0);
			tracker.RecordAlertFired(92.0, T0);

			// Clear
			var clear = tracker.Evaluate(false, 85.0, 90.0, T0.AddMinutes(5));
			tracker.RecordCleared();

			// Re-cross threshold → fires fresh alert
			var reFire = tracker.Evaluate(true, 91.0, 90.0, T0.AddMinutes(10));
			Assert.Equal(AlertAction.Fire, reFire.Action);
		}

		// ── Cooldown + Progression ─────────────────────────────────────

		[Fact]
		public void Cooldown_DoesNotReFireTooSoon()
		{
			var tracker = new ThresholdAlertTracker(
				requiredConsecutive: 1,
				reAlertIncrease: 5.0,
				cooldownInterval: TimeSpan.FromMinutes(60));

			// Fire at 90%
			tracker.Evaluate(true, 90.0, 90.0, T0);
			tracker.RecordAlertFired(90.0, T0);

			// 30 minutes later, usage climbed to 95% — but cooldown not elapsed
			var result = tracker.Evaluate(true, 95.0, 90.0, T0.AddMinutes(30));
			Assert.Equal(AlertAction.None, result.Action);
		}

		[Fact]
		public void Cooldown_PlusProgression_ReFires()
		{
			var tracker = new ThresholdAlertTracker(
				requiredConsecutive: 1,
				reAlertIncrease: 5.0,
				cooldownInterval: TimeSpan.FromMinutes(60));

			// Fire at 90%
			tracker.Evaluate(true, 90.0, 90.0, T0);
			tracker.RecordAlertFired(90.0, T0);

			// 65 minutes later, usage climbed to 95% (90 + 5) — both conditions met
			var result = tracker.Evaluate(true, 95.0, 90.0, T0.AddMinutes(65));
			Assert.Equal(AlertAction.Fire, result.Action);
		}

		[Fact]
		public void Cooldown_ElapsedButNoProgression_DoesNotReFire()
		{
			var tracker = new ThresholdAlertTracker(
				requiredConsecutive: 1,
				reAlertIncrease: 5.0,
				cooldownInterval: TimeSpan.FromMinutes(60));

			// Fire at 90%
			tracker.Evaluate(true, 90.0, 90.0, T0);
			tracker.RecordAlertFired(90.0, T0);

			// 75 minutes later, same usage at 90% — cooldown elapsed but no climb
			var result = tracker.Evaluate(true, 90.0, 90.0, T0.AddMinutes(75));
			Assert.Equal(AlertAction.None, result.Action);
		}

		// ── Debounce + Hysteresis combined ──────────────────────────────

		[Fact]
		public void FiveConsecutive_ThenClear_ThenFiveAgain_FiresTwice()
		{
			var tracker = new ThresholdAlertTracker(requiredConsecutive: 5, hysteresisBand: 5.0);
			var time = T0;

			// First run of 5
			for (int i = 0; i < 5; i++)
			{
				tracker.Evaluate(true, 90.0, 85.0, time);
				time = time.AddSeconds(2);
			}
			tracker.RecordAlertFired(90.0, time);

			// Clear
			tracker.Evaluate(false, 79.0, 85.0, time.AddSeconds(10));
			tracker.RecordCleared();

			time = time.AddSeconds(20);

			// Second run of 5
			AlertEvaluation result = AlertEvaluation.None;
			for (int i = 0; i < 5; i++)
			{
				result = tracker.Evaluate(true, 91.0, 85.0, time);
				time = time.AddSeconds(2);
			}

			Assert.Equal(AlertAction.Fire, result.Action);
		}
	}
}
