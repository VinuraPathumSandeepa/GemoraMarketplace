import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../constants/gemora_colors.dart';
import '../../widgets/gemora_mark.dart';

enum WelcomeAction { explore, signIn }

/// A short, replayable introduction to the destinations in Gemora.
class WelcomeScreen extends StatefulWidget {
  const WelcomeScreen({
    super.key,
    required this.onFinished,
    this.canSignIn = true,
    this.isReplay = false,
  });

  final ValueChanged<WelcomeAction> onFinished;
  final bool canSignIn;
  final bool isReplay;

  @override
  State<WelcomeScreen> createState() => _WelcomeScreenState();
}

class _WelcomeScreenState extends State<WelcomeScreen> {
  final PageController _controller = PageController();
  int _page = 0;
  bool _finished = false;

  static const _slides = <_WelcomeSlide>[
    _WelcomeSlide(
      eyebrow: 'WELCOME TO GEMORA',
      title: 'A world of remarkable gemstones.',
      description: 'Explore Sri Lanka’s gemstone marketplace and find a piece that feels like yours.',
    ),
    _WelcomeSlide(
      eyebrow: 'DISCOVER',
      title: 'Find the gem you love.',
      description: 'Tap Explore to browse gemstones. Search by name or choose a category, then open a listing for its details.',
    ),
    _WelcomeSlide(
      eyebrow: 'YOUR JOURNEY',
      title: 'Make Gemora yours.',
      description: 'Create an account to buy or sell. Your Workspace holds the tools for your role, from orders to listings.',
    ),
    _WelcomeSlide(
      eyebrow: 'QUICK TOUR',
      title: 'Everything in its place.',
      description: 'Home brings you back, Explore shows gems, Workspace opens your tools, and Profile manages your account.',
    ),
  ];

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _finish(WelcomeAction action) {
    if (_finished) return;
    _finished = true;
    widget.onFinished(action);
  }

  void _next() {
    if (_page == _slides.length - 1) {
      _finish(WelcomeAction.explore);
      return;
    }
    _controller.nextPage(
      duration: const Duration(milliseconds: 360),
      curve: Curves.easeOutCubic,
    );
  }

  void _back() {
    _controller.previousPage(
      duration: const Duration(milliseconds: 360),
      curve: Curves.easeOutCubic,
    );
  }

  @override
  Widget build(BuildContext context) {
    final lastPage = _page == _slides.length - 1;

    return Scaffold(
      backgroundColor: GemoraColors.cream,
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 600),
            child: Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(22, 10, 18, 8),
                  child: Row(
                    children: [
                      Container(
                        width: 38,
                        height: 38,
                        decoration: BoxDecoration(
                          color: GemoraColors.darkGreen,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: const Center(child: GemoraMark(size: 29)),
                      ),
                      const SizedBox(width: 10),
                      const Text(
                        'GEMORA',
                        style: TextStyle(
                          color: GemoraColors.darkGreen,
                          fontSize: 17,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 2,
                        ),
                      ),
                      const Spacer(),
                      TextButton(
                        key: const Key('welcome_skip'),
                        onPressed: () => _finish(WelcomeAction.explore),
                        child: Text(widget.isReplay ? 'Close' : 'Skip'),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: PageView.builder(
                    key: const Key('welcome_pages'),
                    controller: _controller,
                    itemCount: _slides.length,
                    onPageChanged: (index) => setState(() => _page = index),
                    itemBuilder: (context, index) =>
                        _WelcomePage(slide: _slides[index], index: index),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(24, 8, 24, 18),
                  child: Column(
                    children: [
                      Semantics(
                        label: 'Welcome page ${_page + 1} of ${_slides.length}',
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: List.generate(_slides.length, (index) {
                            final selected = index == _page;
                            return AnimatedContainer(
                              duration: const Duration(milliseconds: 250),
                              width: selected ? 27 : 7,
                              height: 7,
                              margin: const EdgeInsets.symmetric(horizontal: 4),
                              decoration: BoxDecoration(
                                color: selected
                                    ? GemoraColors.darkGold
                                    : GemoraColors.border,
                                borderRadius: BorderRadius.circular(10),
                              ),
                            );
                          }),
                        ),
                      ),
                      const SizedBox(height: 20),
                      SizedBox(
                        width: double.infinity,
                        height: 54,
                        child: FilledButton(
                          key: const Key('welcome_next'),
                          onPressed: _next,
                          style: FilledButton.styleFrom(
                            backgroundColor: GemoraColors.darkGreen,
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(16),
                            ),
                          ),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Flexible(
                                child: FittedBox(
                                  fit: BoxFit.scaleDown,
                                  child: Text(
                                    lastPage ? 'Explore Gemora' : 'Continue',
                                    style: const TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                ),
                              ),
                              const SizedBox(width: 10),
                              const Icon(Icons.arrow_forward_rounded, size: 19),
                            ],
                          ),
                        ),
                      ),
                      SizedBox(
                        height: 42,
                        child: Row(
                          mainAxisAlignment: lastPage
                              ? MainAxisAlignment.spaceBetween
                              : MainAxisAlignment.center,
                          children: [
                            if (_page > 0)
                              Flexible(
                                child: TextButton(
                                  onPressed: _back,
                                  child: const Text('Back'),
                                ),
                              ),
                            if (lastPage && widget.canSignIn)
                              Flexible(
                                child: TextButton(
                                  key: const Key('welcome_sign_in'),
                                  onPressed: () =>
                                      _finish(WelcomeAction.signIn),
                                  child: const FittedBox(
                                    fit: BoxFit.scaleDown,
                                    child: Text('Sign in instead'),
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _WelcomeSlide {
  const _WelcomeSlide({
    required this.eyebrow,
    required this.title,
    required this.description,
  });

  final String eyebrow;
  final String title;
  final String description;
}

class _WelcomePage extends StatelessWidget {
  const _WelcomePage({required this.slide, required this.index});

  final _WelcomeSlide slide;
  final int index;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final heroHeight = math.max(215.0, constraints.maxHeight * 0.52);
        return SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: ConstrainedBox(
            constraints: BoxConstraints(minHeight: constraints.maxHeight),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                SizedBox(
                  height: heroHeight,
                  width: double.infinity,
                  child: _WelcomeIllustration(index: index),
                ),
                const SizedBox(height: 25),
                Text(
                  slide.eyebrow,
                  style: const TextStyle(
                    color: GemoraColors.darkGold,
                    fontSize: 11,
                    fontWeight: FontWeight.w900,
                    letterSpacing: 2.2,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  slide.title,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: GemoraColors.darkGreen,
                    fontSize: 29,
                    height: 1.1,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  slide.description,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: GemoraColors.mutedText,
                    fontSize: 14,
                    height: 1.55,
                  ),
                ),
                const SizedBox(height: 12),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _WelcomeIllustration extends StatelessWidget {
  const _WelcomeIllustration({required this.index});

  final int index;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [GemoraColors.darkGreen, GemoraColors.green],
        ),
        borderRadius: BorderRadius.circular(32),
      ),
      child: Stack(
        alignment: Alignment.center,
        children: [
          Positioned(
            right: -35,
            top: -55,
            child: Container(
              width: 170,
              height: 170,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: Colors.white.withValues(alpha: 0.08)),
              ),
            ),
          ),
          Positioned(
            left: -40,
            bottom: -75,
            child: Container(
              width: 190,
              height: 190,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: Colors.white.withValues(alpha: 0.08)),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(20),
            child: switch (index) {
              0 => const _GemHero(),
              1 => const _ExploreHero(),
              2 => const _WorkspaceHero(),
              _ => const _NavigationHero(),
            },
          ),
        ],
      ),
    );
  }
}

class _GemHero extends StatelessWidget {
  const _GemHero();

  @override
  Widget build(BuildContext context) {
    return Stack(
      alignment: Alignment.center,
      children: [
        Container(
          width: 190,
          height: 190,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: Border.all(
              color: GemoraColors.gold.withValues(alpha: 0.28),
            ),
          ),
        ),
        Container(
          width: 145,
          height: 145,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: GemoraColors.gold.withValues(alpha: 0.1),
          ),
        ),
        const GemoraMark(size: 125),
        const Positioned(
          top: 18,
          right: 35,
          child: Icon(Icons.auto_awesome, color: GemoraColors.gold, size: 23),
        ),
      ],
    );
  }
}

class _ExploreHero extends StatelessWidget {
  const _ExploreHero();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 315),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 13),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16),
              ),
              child: const Row(
                children: [
                  Icon(Icons.search, color: GemoraColors.green, size: 22),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Search gemstones',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: GemoraColors.mutedText),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: _GemTile(label: 'SAPPHIRE', color: Color(0xFF3A6A93)),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _GemTile(label: 'RUBY', color: Color(0xFF9B4D5A)),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _GemTile extends StatelessWidget {
  const _GemTile({required this.label, required this.color});
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 104,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.diamond, color: color, size: 52),
          const SizedBox(height: 7),
          Text(
            label,
            style: const TextStyle(
              color: GemoraColors.darkGreen,
              fontSize: 10,
              fontWeight: FontWeight.w900,
              letterSpacing: 1.1,
            ),
          ),
        ],
      ),
    );
  }
}

class _WorkspaceHero extends StatelessWidget {
  const _WorkspaceHero();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 295),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  CircleAvatar(
                    radius: 18,
                    backgroundColor: GemoraColors.subtleSurface,
                    child: Icon(Icons.work_outline, color: GemoraColors.green),
                  ),
                  SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'Your Workspace',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: GemoraColors.darkGreen,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              _toolRow(Icons.inventory_2_outlined, 'Listings & orders'),
              const SizedBox(height: 7),
              _toolRow(Icons.verified_outlined, 'Verification tools'),
            ],
          ),
        ),
      ),
    );
  }

  Widget _toolRow(IconData icon, String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      decoration: BoxDecoration(
        color: GemoraColors.cream,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(icon, color: GemoraColors.darkGold, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              text,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: GemoraColors.darkGreen,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _NavigationHero extends StatelessWidget {
  const _NavigationHero();

  @override
  Widget build(BuildContext context) {
    const items = <(IconData, String)>[
      (Icons.home_outlined, 'Home'),
      (Icons.diamond_outlined, 'Explore'),
      (Icons.work_outline, 'Workspace'),
      (Icons.person_outline, 'Profile'),
    ];
    return Center(
      child: Container(
        padding: const EdgeInsets.fromLTRB(12, 23, 12, 18),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(22),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: [
            for (final item in items)
              Flexible(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(item.$1, color: GemoraColors.green, size: 25),
                    const SizedBox(height: 8),
                    FittedBox(
                      fit: BoxFit.scaleDown,
                      child: Text(
                        item.$2,
                        style: const TextStyle(
                          color: GemoraColors.darkGreen,
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
