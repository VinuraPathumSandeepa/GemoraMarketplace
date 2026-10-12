import 'dart:ui';

import 'package:flutter/material.dart';

import '../constants/gemora_colors.dart';

class BuyerGradientBackground extends StatelessWidget {
  final Widget child;

  const BuyerGradientBackground({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFFF7F1E5), Color(0xFFF5F8F4), Color(0xFFE7F0EB)],
          stops: [0, .52, 1],
        ),
      ),
      child: Stack(
        children: [
          const Positioned(
            top: -90,
            right: -70,
            child: _GlowOrb(size: 220, color: Color(0x4073AD93)),
          ),
          const Positioned(
            bottom: 50,
            left: -90,
            child: _GlowOrb(size: 250, color: Color(0x35E4BC74)),
          ),
          Positioned.fill(child: child),
        ],
      ),
    );
  }
}

class _GlowOrb extends StatelessWidget {
  final double size;
  final Color color;

  const _GlowOrb({required this.size, required this.color});

  @override
  Widget build(BuildContext context) {
    return ImageFiltered(
      imageFilter: ImageFilter.blur(sigmaX: 34, sigmaY: 34),
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      ),
    );
  }
}

class BuyerGlassPanel extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry padding;
  final EdgeInsetsGeometry? margin;
  final BorderRadius borderRadius;
  final VoidCallback? onTap;
  final Color? tint;

  const BuyerGlassPanel({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(18),
    this.margin,
    this.borderRadius = const BorderRadius.all(Radius.circular(24)),
    this.onTap,
    this.tint,
  });

  @override
  Widget build(BuildContext context) {
    final panel = ClipRRect(
      borderRadius: borderRadius,
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 220),
          padding: padding,
          decoration: BoxDecoration(
            color: tint ?? Colors.white.withValues(alpha: .68),
            borderRadius: borderRadius,
            border: Border.all(color: Colors.white.withValues(alpha: .82)),
            boxShadow: [
              BoxShadow(
                color: GemoraColors.darkGreen.withValues(alpha: .08),
                blurRadius: 26,
                offset: const Offset(0, 12),
              ),
            ],
          ),
          child: Material(color: Colors.transparent, child: child),
        ),
      ),
    );

    return Padding(
      padding: margin ?? EdgeInsets.zero,
      child: onTap == null
          ? panel
          : Material(
              color: Colors.transparent,
              child: InkWell(
                borderRadius: borderRadius,
                onTap: onTap,
                child: panel,
              ),
            ),
    );
  }
}

class BuyerSectionHeading extends StatelessWidget {
  final String eyebrow;
  final String title;
  final String? description;
  final Widget? trailing;

  const BuyerSectionHeading({
    super.key,
    required this.eyebrow,
    required this.title,
    this.description,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                eyebrow.toUpperCase(),
                style: const TextStyle(
                  color: GemoraColors.darkGold,
                  fontSize: 10,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 1.35,
                ),
              ),
              const SizedBox(height: 7),
              Text(
                title,
                style: const TextStyle(
                  color: GemoraColors.darkGreen,
                  fontSize: 23,
                  fontWeight: FontWeight.w900,
                  letterSpacing: -.4,
                ),
              ),
              if (description != null) ...[
                const SizedBox(height: 6),
                Text(
                  description!,
                  style: const TextStyle(
                    color: GemoraColors.mutedText,
                    height: 1.45,
                    fontSize: 12,
                  ),
                ),
              ],
            ],
          ),
        ),
        if (trailing != null) ...[const SizedBox(width: 12), trailing!],
      ],
    );
  }
}
