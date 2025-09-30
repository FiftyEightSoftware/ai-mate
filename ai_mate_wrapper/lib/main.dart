import 'package:flutter/material.dart';
import 'package:webview_flutter/webview_flutter.dart';
import 'dart:io';
import 'package:flutter/foundation.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const AIMateWrapper());
}

class AIMateWrapper extends StatelessWidget {
  const AIMateWrapper({super.key});

  static const Color copper = Color(0xFFC8793E);

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AI-Mate',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: copper, brightness: Brightness.dark),
        useMaterial3: true,
      ),
      home: const AIMateWebView(),
    );
  }
}

class AIMateWebView extends StatefulWidget {
  const AIMateWebView({super.key});

  @override
  State<AIMateWebView> createState() => _AIMateWebViewState();
}

class _AIMateWebViewState extends State<AIMateWebView> {
  late final WebViewController _controller;
  // Development server URL
  late final String devUrl = Platform.isAndroid
      ? 'http://10.0.2.2:5173'
      : 'http://localhost:5173';
  // Production URL to use in release builds
  static const String releaseUrl = 'https://ai-mate.nickphinesme.com';
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(NavigationDelegate(
        onPageStarted: (_) => setState(() => _isLoading = true),
        onPageFinished: (_) => setState(() => _isLoading = false),
      ))
      ..loadRequest(Uri.parse(kReleaseMode ? releaseUrl : devUrl));
  }

  Future<void> _reload() async {
    await _controller.reload();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0B0E12),
      appBar: AppBar(
        title: const Text('AI-Mate'),
        backgroundColor: const Color(0xFF14181D),
      ),
      body: RefreshIndicator(
        color: AIMateWrapper.copper,
        onRefresh: _reload,
        child: Stack(
          children: [
            WebViewWidget(controller: _controller),
            if (_isLoading)
              const LinearProgressIndicator(minHeight: 2),
          ],
        ),
      ),
    );
  }
}
