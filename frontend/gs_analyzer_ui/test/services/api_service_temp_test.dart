import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

void main() {
  group('ApiService.getTempPreview', () {
    test('GETs /api/tempfiles/preview and parses response', () async {
      late Uri capturedUrl;
      late String capturedMethod;

      final client = MockClient((req) async {
        capturedUrl = req.url;
        capturedMethod = req.method;
        return http.Response(
          jsonEncode({
            'success': true,
            'data': {
              'totalBytes': 1024,
              'totalFormatted': '1 KB',
              'locations': [],
            },
          }),
          200,
        );
      });

      final apiService = ApiService(client);
      final result = await apiService.getTempPreview();

      expect(capturedMethod, 'GET');
      expect(capturedUrl.path, endsWith('/api/tempfiles/preview'));
      expect(result.totalBytes, 1024);
      expect(result.totalFormatted, '1 KB');
    });

    test('throws on non-200', () async {
      final client = MockClient(
        (_) async => http.Response('Server Error', 500),
      );
      final apiService = ApiService(client);

      expect(() => apiService.getTempPreview(), throwsException);
    });
  });

  group('ApiService.cleanTempFiles', () {
    test('POSTs to /api/tempfiles/clean with paths in body', () async {
      late Uri capturedUrl;
      late String capturedMethod;
      late String capturedBody;

      final client = MockClient((req) async {
        capturedUrl = req.url;
        capturedMethod = req.method;
        capturedBody = req.body;
        return http.Response(
          jsonEncode({
            'success': true,
            'data': {
              'deletedFiles': 10,
              'freedBytes': 2048,
              'freedFormatted': '2 KB',
              'skippedFiles': 1,
            },
          }),
          200,
        );
      });

      final apiService = ApiService(client);
      final paths = ['C:\\temp', 'C:\\cache'];
      final result = await apiService.cleanTempFiles(paths);

      expect(capturedMethod, 'POST');
      expect(capturedUrl.path, endsWith('/api/tempfiles/clean'));

      final bodyMap = jsonDecode(capturedBody);
      expect(bodyMap['paths'], equals(paths));

      expect(result.deletedFiles, 10);
      expect(result.freedFormatted, '2 KB');
      expect(result.skippedFiles, 1);
    });

    test('throws on non-200', () async {
      final client = MockClient((_) async => http.Response('Bad Request', 400));
      final apiService = ApiService(client);

      expect(() => apiService.cleanTempFiles(['C:\\temp']), throwsException);
    });
  });
}
