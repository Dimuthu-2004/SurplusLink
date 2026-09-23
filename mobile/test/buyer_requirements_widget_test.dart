import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/requirements/requirement_models.dart';

import 'support/fakes.dart';
import 'support/requirement_fakes.dart';

void main() {
  testWidgets(
    'buyer home exposes requirements and guest seller manager routes are protected',
    (tester) async {
      final gateway = FakeRequirements();
      await pumpApp(tester, gateway);
      expect(find.byKey(const Key('open-my-requirements')), findsOneWidget);
      await tester.tap(find.byKey(const Key('open-my-requirements')));
      await tester.pumpAndSettle();
      expect(find.text('My Requirements'), findsOneWidget);
      expect(find.textContaining('No requirements found'), findsOneWidget);
      for (final user in <AppUser?>[
        null,
        sellerUser,
        const AppUser(
          id: 'm1',
          email: 'manager@test',
          roles: [AppRole.manager],
        ),
      ]) {
        for (final path in [
          '/requirements',
          '/requirements/new',
          '/requirements/r1',
          '/requirements/r1/edit',
          '/requirements/r1/status',
          '/requirements/r1/history',
        ]) {
          final blocked = FakeRequirements();
          await pumpApp(tester, blocked, user: user, path: path);
          expect(find.byKey(const Key('open-my-requirements')), findsNothing);
          expect(blocked.reads, 0);
          expect(blocked.queries, isEmpty);
          expect(blocked.historyPages, isEmpty);
          expect(find.byKey(const Key('requirement-save')), findsNothing);
        }
      }
    },
  );

  testWidgets(
    'list loading empty error retry and search preserve query intent',
    (tester) async {
      final gateway = FakeRequirements()..pendingList = Completer();
      await pumpApp(tester, gateway, path: '/requirements', settle: false);
      await tester.pump();
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      gateway.pendingList!.complete(
        const RequirementPage(items: [], total: 0, page: 1, pageSize: 10),
      );
      gateway.pendingList = null;
      await tester.pumpAndSettle();
      expect(find.textContaining('No requirements found'), findsOneWidget);
      await tester.enterText(
        find.byKey(const Key('requirements-search')),
        'urgent cement',
      );
      gateway.error = const ApiException('Unable to connect.');
      await tester.tap(find.byKey(const Key('requirements-apply')));
      await tester.pumpAndSettle();
      expect(find.text('Unable to connect.'), findsOneWidget);
      gateway.error = null;
      gateway.items = [testRequirement()];
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(gateway.queries.last.search, 'urgent cement');
      expect(find.text('1 requirements'), findsOneWidget);
      await tester.tap(find.text('Filter and sort'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('sort-createdAt')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Budget').last);
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('direction-desc')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Ascending').last);
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('requirements-apply')));
      await tester.pumpAndSettle();
      expect(gateway.queries.last.sort, 'budget');
      expect(gateway.queries.last.sortDir, 'asc');
      expect(gateway.queries.last.page, 1);
    },
  );

  testWidgets(
    'create validates fields date picker GPS precision and disables duplicate save',
    (tester) async {
      final gateway = FakeRequirements()..pendingSave = Completer();
      final location = FakeRequirementLocation();
      await pumpApp(
        tester,
        gateway,
        path: '/requirements/new',
        location: location,
      );
      await tester.tap(find.byKey(const Key('requirement-save')));
      await tester.pumpAndSettle();
      expect(gateway.saves, 0);
      expect(find.text('Choose a category.'), findsOneWidget);
      await fillForm(tester);
      await tester.enterText(
        find.byKey(const Key('requirement-quantity')),
        '0.0001',
      );
      await tester.tap(find.byKey(const Key('requirement-save')));
      await tester.pumpAndSettle();
      expect(find.text('Use at most 3 decimal places.'), findsOneWidget);
      expect(gateway.saves, 0);
      await tester.enterText(
        find.byKey(const Key('requirement-quantity')),
        '12.125',
      );
      await tester.tap(find.byKey(const Key('requirement-deadline')));
      await tester.pumpAndSettle();
      expect(find.byType(DatePickerDialog), findsOneWidget);
      await tester.tap(find.text('OK'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('requirement-gps')));
      await tester.pumpAndSettle();
      expect(location.calls, 1);
      expect(
        tester
            .widget<TextFormField>(
              find.byKey(const Key('requirement-latitude')),
            )
            .controller!
            .text,
        '6.123457',
      );
      await tester.tap(find.byKey(const Key('requirement-save')));
      await tester.pump();
      expect(find.text('Saving…'), findsOneWidget);
      await tester.tap(find.byKey(const Key('requirement-save')));
      expect(gateway.saves, 1);
      expect(gateway.saved!.deadline.hour, 23);
      expect(gateway.saved!.requiredQuantity, 12.125);
      gateway.pendingSave!.complete(testRequirement());
      await tester.pumpAndSettle();
      expect(find.text('Requirement Details'), findsOneWidget);
    },
  );

  testWidgets(
    'category change reloads the unit dropdown and blocks save without active units',
    (tester) async {
      final gateway = FakeRequirements()
        ..categoryItems = const [
          RequirementCategory('c1', 'Cement'),
          RequirementCategory('c2', 'Tiles'),
        ]
        ..unitsByCategory = {
          'c1': ['kg'],
          'c2': [],
        };
      await pumpApp(tester, gateway, path: '/requirements/new');
      await tester.tap(find.byKey(const Key('requirement-category')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cement').last);
      await tester.pumpAndSettle();
      expect(gateway.unitRequests, ['c1']);
      await selectUnit(tester, 'kg');
      await tester.tap(find.byKey(const Key('requirement-category')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Tiles').last);
      await tester.pumpAndSettle();
      expect(gateway.unitRequests, ['c1', 'c2']);
      expect(find.text('No available units for this category'), findsOneWidget);
      expect(
        tester
            .widget<FilledButton>(find.byKey(const Key('requirement-save')))
            .onPressed,
        isNull,
      );
    },
  );
  testWidgets('GPS denial and save errors retain entered values for retry', (
    tester,
  ) async {
    final gateway = FakeRequirements()
      ..saveError = const ApiException(
        'Deadline must be in the future.',
        statusCode: 400,
      );
    final location = FakeRequirementLocation()
      ..error = const LocationCaptureException(
        'Permission denied. Enter coordinates manually.',
      );
    await pumpApp(
      tester,
      gateway,
      path: '/requirements/new',
      location: location,
    );
    await fillForm(tester);
    await tester.tap(find.byKey(const Key('requirement-gps')));
    await tester.pumpAndSettle();
    expect(find.textContaining('Permission denied'), findsOneWidget);
    await tester.enterText(find.byKey(const Key('requirement-latitude')), '6');
    await tester.enterText(
      find.byKey(const Key('requirement-longitude')),
      '79',
    );
    await tester.tap(find.byKey(const Key('requirement-save')));
    await tester.pumpAndSettle();
    expect(find.text('Deadline must be in the future.'), findsOneWidget);
    expect(
      tester
          .widget<TextFormField>(find.byKey(const Key('requirement-quantity')))
          .controller!
          .text,
      '12',
    );
  });

  testWidgets('draft edit is prefilled and open edit is blocked', (
    tester,
  ) async {
    final gateway = FakeRequirements();
    await pumpApp(tester, gateway, path: '/requirements/r1/edit');
    expect(
      tester
          .widget<TextFormField>(find.byKey(const Key('requirement-notes')))
          .controller!
          .text,
      'Deliver to site',
    );
    await tester.enterText(
      find.byKey(const Key('requirement-notes')),
      'Updated note',
    );
    await tester.tap(find.byKey(const Key('requirement-save')));
    await tester.pumpAndSettle();
    expect(gateway.saved!.notes, 'Updated note');
    gateway.row = testRequirement(status: 'OPEN');
    await pumpApp(tester, gateway, path: '/requirements/r1/edit');
    expect(find.textContaining('Only draft requirements'), findsOneWidget);
    expect(find.byKey(const Key('requirement-save')), findsNothing);
  });

  testWidgets(
    'submit confirms transition and deferred start keeps open with actionable error',
    (tester) async {
      final gateway = FakeRequirements()
        ..startError = const ApiException(
          'Matching is not available yet. Your requirement remains open.',
          statusCode: 503,
        );
      await pumpApp(tester, gateway, path: '/requirements/r1');
      await tester.tap(find.byKey(const Key('requirement-submit')));
      await tester.pumpAndSettle();
      expect(gateway.submits, 0);
      await tester.tap(find.byKey(const Key('confirm-requirement-action')));
      await tester.pumpAndSettle();
      expect(gateway.submits, 1);
      expect(find.byKey(const Key('requirement-edit')), findsNothing);
      await tester.tap(find.byKey(const Key('requirement-start')));
      await tester.pumpAndSettle();
      expect(find.textContaining('does not reserve'), findsOneWidget);
      await tester.tap(find.byKey(const Key('confirm-requirement-action')));
      await tester.pumpAndSettle();
      expect(
        find.textContaining('Your requirement remains open.'),
        findsOneWidget,
      );
      expect(find.text('Open'), findsOneWidget);
      expect(gateway.starts, 1);
      await tester.tap(find.byKey(const Key('requirement-cancel')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('confirm-requirement-action')));
      await tester.pumpAndSettle();
      expect(find.text('Cancelled'), findsOneWidget);
      expect(find.byKey(const Key('requirement-start')), findsNothing);
    },
  );

  testWidgets(
    'successful start shows only returned workflow and recommendation availability',
    (tester) async {
      final gateway = FakeRequirements()..row = testRequirement(status: 'OPEN');
      await pumpApp(tester, gateway, path: '/requirements/r1');
      await tester.tap(find.byKey(const Key('requirement-start')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('confirm-requirement-action')));
      await tester.pumpAndSettle();
      expect(find.text('Matching Status'), findsOneWidget);
      expect(find.text('Workflow ID: workflow-from-api'), findsOneWidget);
      expect(
        find.textContaining('Recommendation details are not available'),
        findsOneWidget,
      );
      gateway.row = testRequirement(status: 'MATCH_FOUND');
      await tester.tap(find.text('Refresh status'));
      await tester.pumpAndSettle();
      expect(find.text('Match Found'), findsOneWidget);
    },
  );

  testWidgets(
    'history shows transitions and navigates pages with error and empty states',
    (tester) async {
      final gateway = FakeRequirements()
        ..historyData = RequirementPage(
          items: [
            RequirementHistoryEntry(
              id: 'a1',
              action: 'STATUS_CHANGED',
              fromStatus: 'DRAFT',
              toStatus: 'OPEN',
              createdAt: DateTime.utc(2026),
              actorUserId: buyerUser.id,
            ),
          ],
          total: 21,
          page: 1,
          pageSize: 20,
        );
      await pumpApp(tester, gateway, path: '/requirements/r1/history');
      expect(find.text('Draft → Open'), findsOneWidget);
      await tester.tap(find.byTooltip('Next page'));
      await tester.pumpAndSettle();
      expect(gateway.historyPages.last, 2);
      expect(find.text('Page 2 of 2'), findsOneWidget);
      gateway.error = const ApiException('Not allowed', statusCode: 403);
      await tester.tap(find.byTooltip('Refresh history'));
      await tester.pumpAndSettle();
      expect(
        find.text('You do not have access to this requirement.'),
        findsOneWidget,
      );
      gateway.error = null;
      gateway.historyData = const RequirementPage(
        items: [],
        total: 0,
        page: 1,
        pageSize: 20,
      );
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(find.text('No history events on this page.'), findsOneWidget);
    },
  );

  testWidgets(
    'small screen large text has no overflow and expired draft cannot submit',
    (tester) async {
      final gateway = FakeRequirements()
        ..row = testRequirement(deadline: DateTime.utc(2020));
      await pumpApp(tester, gateway, path: '/requirements/r1', small: true);
      expect(tester.takeException(), isNull);
      await tester.scrollUntilVisible(
        find.byKey(const Key('requirement-submit')),
        300,
        scrollable: find.byType(Scrollable).first,
      );
      final submit = tester.widget<FilledButton>(
        find.byKey(const Key('requirement-submit')),
      );
      expect(submit.onPressed, isNull);
      expect(tester.takeException(), isNull);
    },
  );
}

Future<void> pumpApp(
  WidgetTester tester,
  FakeRequirements gateway, {
  String path = '/home',
  AppUser? user = buyerUser,
  FakeRequirementLocation? location,
  bool settle = true,
  bool small = false,
}) async {
  await tester.pumpWidget(const SizedBox.shrink());
  tester.view.physicalSize = small
      ? const Size(390, 844)
      : const Size(1000, 1800);
  tester.view.devicePixelRatio = 1;
  tester.platformDispatcher.textScaleFactorTestValue = small ? 1.8 : 1;
  addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final auth = AuthController(FakeAuthGateway()..restoredUser = user);
  await tester.pumpWidget(
    SurplusLinkApp(
      key: UniqueKey(),
      authController: auth,
      requirementGateway: gateway,
      requirementLocation: location ?? FakeRequirementLocation(),
      initialLocation: path,
    ),
  );
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }
}

Future<void> selectUnit(WidgetTester tester, String unit) async {
  await tester.tap(find.byKey(const Key('requirement-unit')));
  await tester.pumpAndSettle();
  await tester.tap(find.text(unit).last);
  await tester.pumpAndSettle();
}

Future<void> fillForm(WidgetTester tester) async {
  await tester.tap(find.byKey(const Key('requirement-category')));
  await tester.pumpAndSettle();
  await tester.tap(find.text('Cement').last);
  await tester.pumpAndSettle();
  await tester.enterText(find.byKey(const Key('requirement-quantity')), '12');
  await selectUnit(tester, 'kg');
  await tester.enterText(find.byKey(const Key('requirement-budget')), '120.50');
}
