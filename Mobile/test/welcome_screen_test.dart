import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/screens/onboarding/welcome_screen.dart';

void main() {
  testWidgets('welcome tour explains navigation and finishes with Explore', (
    tester,
  ) async {
    WelcomeAction? chosenAction;
    await tester.pumpWidget(
      MaterialApp(
        home: WelcomeScreen(onFinished: (action) => chosenAction = action),
      ),
    );

    expect(find.text('A world of remarkable gemstones.'), findsOneWidget);
    expect(find.text('Skip'), findsOneWidget);

    for (var page = 0; page < 3; page++) {
      await tester.tap(find.byKey(const Key('welcome_next')));
      await tester.pumpAndSettle();
    }

    expect(find.text('Everything in its place.'), findsOneWidget);
    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Explore'), findsOneWidget);
    expect(find.text('Workspace'), findsOneWidget);
    expect(find.text('Profile'), findsOneWidget);

    await tester.tap(find.byKey(const Key('welcome_next')));
    expect(chosenAction, WelcomeAction.explore);
  });

  testWidgets('skip and replay close are available immediately', (
    tester,
  ) async {
    WelcomeAction? chosenAction;
    await tester.pumpWidget(
      MaterialApp(
        home: WelcomeScreen(
          isReplay: true,
          onFinished: (action) => chosenAction = action,
        ),
      ),
    );

    expect(find.text('Close'), findsOneWidget);
    await tester.tap(find.byKey(const Key('welcome_skip')));
    expect(chosenAction, WelcomeAction.explore);
  });

  testWidgets('tour stays usable on a compact phone', (tester) async {
    tester.view.physicalSize = const Size(320, 568);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(home: WelcomeScreen(onFinished: (_) {})),
    );
    for (var page = 0; page < 4; page++) {
      if (page < 3) {
        await tester.tap(find.byKey(const Key('welcome_next')));
        await tester.pumpAndSettle();
      }
    }
  });
}
