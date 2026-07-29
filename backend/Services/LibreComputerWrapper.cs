using LibreHardwareMonitor.Hardware;
using IComputerEngine = GSSystemAnalyzer.Interfaces.IComputerEngine;

namespace GSSystemAnalyzer.Services
{
	public class LibreComputerWrapper : IComputerEngine
	{
		private readonly Computer _computer;

		public LibreComputerWrapper()
		{
			_computer = new Computer
			{
				IsCpuEnabled = true,
				IsGpuEnabled = true,
				IsMotherboardEnabled = true,
				IsControllerEnabled = true,
				IsStorageEnabled = true
			};
		}

		public IList<IHardware> Hardware => _computer.Hardware;
		public void Accept(IVisitor visitor) => _computer.Accept(visitor);
		public void Open() => _computer.Open();
		public void Close() => _computer.Close();
	}
}
