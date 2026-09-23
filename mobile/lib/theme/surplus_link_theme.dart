import 'package:flutter/material.dart';

/// Shared navy + orange presentation system used by every Flutter screen.
abstract final class SurplusLinkTheme {
  static const background = Color(0xFFF4F7FB);
  static const surface = Color(0xFFFFFFFF);
  static const surfaceSoft = Color(0xFFF8FAFC);
  static const slate900 = Color(0xFF0B1F3A);
  static const slate800 = Color(0xFF1E293B);
  static const slate700 = Color(0xFF334155);
  static const slate600 = Color(0xFF475569);
  static const slate300 = Color(0xFFCBD5E1);
  static const slate200 = Color(0xFFE2E8F0);
  static const amber = Color(0xFFF47B20);
  static const amberDark = Color(0xFFD65F0C);
  static const amberSoft = Color(0xFFFFF0E4);

  static ThemeData get light {
    final scheme = ColorScheme.fromSeed(
      seedColor: slate900,
      brightness: Brightness.light,
      primary: slate900,
      onPrimary: Colors.white,
      secondary: amber,
      onSecondary: Colors.white,
      surface: surface,
      onSurface: slate800,
      error: const Color(0xFFB91C1C),
    );
    final text = Typography.material2021().black.apply(
      bodyColor: slate800,
      displayColor: slate900,
      fontFamily: 'Inter',
    );
    const outline = OutlineInputBorder(
      borderRadius: BorderRadius.all(Radius.circular(12)),
      borderSide: BorderSide(color: slate300),
    );
    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: background,
      textTheme: text,
      appBarTheme: const AppBarTheme(
        backgroundColor: surface,
        foregroundColor: slate900,
        elevation: 0,
        scrolledUnderElevation: 1,
        surfaceTintColor: Colors.transparent,
        centerTitle: false,
        titleTextStyle: TextStyle(fontSize: 20, fontWeight: FontWeight.w800, color: slate900),
        iconTheme: IconThemeData(color: slate700),
      ),
      cardTheme: const CardThemeData(
        color: surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(16)),
          side: BorderSide(color: slate200),
        ),
      ),
      inputDecorationTheme: const InputDecorationTheme(
        filled: true,
        fillColor: surface,
        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        labelStyle: TextStyle(color: slate700, fontWeight: FontWeight.w600),
        hintStyle: TextStyle(color: slate600),
        prefixIconColor: slate600,
        border: outline,
        enabledBorder: outline,
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.all(Radius.circular(12)),
          borderSide: BorderSide(color: slate900, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.all(Radius.circular(12)),
          borderSide: BorderSide(color: Color(0xFFB91C1C)),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.all(Radius.circular(12)),
          borderSide: BorderSide(color: Color(0xFFB91C1C), width: 2),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: slate900,
          foregroundColor: Colors.white,
          minimumSize: const Size(48, 48),
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 13),
          shape: const RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(12))),
          textStyle: const TextStyle(fontWeight: FontWeight.w800),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: slate700,
          minimumSize: const Size(48, 48),
          side: const BorderSide(color: slate300),
          shape: const RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(12))),
          textStyle: const TextStyle(fontWeight: FontWeight.w700),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: amberDark,
          minimumSize: const Size(44, 44),
          textStyle: const TextStyle(fontWeight: FontWeight.w700),
        ),
      ),
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: amber,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(16))),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: surfaceSoft,
        side: const BorderSide(color: slate200),
        labelStyle: const TextStyle(color: slate700, fontWeight: FontWeight.w700),
        shape: const RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(99))),
      ),
      snackBarTheme: const SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: slate800,
        contentTextStyle: TextStyle(color: Colors.white),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(12))),
      ),
      dialogTheme: const DialogThemeData(
        backgroundColor: surface,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(20))),
      ),
      dividerTheme: const DividerThemeData(color: slate200),
      progressIndicatorTheme: const ProgressIndicatorThemeData(color: amber),
    );
  }
}
