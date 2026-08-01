class CpuData {
  final String title;
  final double percentage;
  final double change;
  final List<double> cores;

  const CpuData({
    required this.title,
    required this.percentage,
    required this.change,
    required this.cores
  });
}