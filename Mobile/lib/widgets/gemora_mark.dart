import 'package:flutter/material.dart';

import '../constants/gemora_colors.dart';

/// A small vector mark shared by the splash, onboarding, and marketplace.
class GemoraMark extends StatelessWidget {
  const GemoraMark({super.key, this.size = 56, this.color = GemoraColors.gold});

  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: CustomPaint(painter: _GemoraMarkPainter(color)),
    );
  }
}

class _GemoraMarkPainter extends CustomPainter {
  const _GemoraMarkPainter(this.color);

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    canvas.save();
    canvas.scale(size.width / 100, size.height / 100);

    final outline = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3.5
      ..strokeJoin = StrokeJoin.round
      ..strokeCap = StrokeCap.round;
    final facets = Paint()
      ..color = color.withValues(alpha: 0.72)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2
      ..strokeJoin = StrokeJoin.round;
    final fill = Paint()..color = color.withValues(alpha: 0.13);

    final gem = Path()
      ..moveTo(27, 24)
      ..lineTo(73, 24)
      ..lineTo(85, 40)
      ..lineTo(50, 82)
      ..lineTo(15, 40)
      ..close();
    canvas.drawPath(gem, fill);
    canvas.drawPath(gem, outline);

    final cut = Path()
      ..moveTo(15, 40)
      ..lineTo(85, 40)
      ..moveTo(27, 24)
      ..lineTo(37, 40)
      ..lineTo(50, 82)
      ..moveTo(73, 24)
      ..lineTo(63, 40)
      ..lineTo(50, 82)
      ..moveTo(37, 40)
      ..lineTo(63, 40);
    canvas.drawPath(cut, facets);

    final shine = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.4
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(const Offset(79, 12), const Offset(79, 21), shine);
    canvas.drawLine(const Offset(74.5, 16.5), const Offset(83.5, 16.5), shine);
    canvas.restore();
  }

  @override
  bool shouldRepaint(covariant _GemoraMarkPainter oldDelegate) =>
      oldDelegate.color != color;
}
