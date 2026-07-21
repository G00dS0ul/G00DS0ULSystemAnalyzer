using LibreHardwareMonitor.Hardware;

namespace GSSystemAnalyzer.Interfaces
{
	public interface IComputerEngine
	{
		IList<IHardware> Hardware { get; }
		void Accept(IVisitor visitor);
		void Open();
		void Close();
	}
}
