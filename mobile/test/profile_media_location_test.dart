import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:image_picker/image_picker.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/auth/auth_repository.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_media.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/screens/material_listing_form_screen.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';
import 'package:mobile/widgets/location_card.dart';
import 'package:mobile/widgets/profile_fields.dart';

import 'category_forms_test.dart' show FakeCategoryMaterials;
import 'support/fakes.dart';

final photoBytes = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j5o8AAAAASUVORK5CYII=',
);

void main() {
  test('API location lookup sends coordinates to the SurplusLink endpoint', () async {
    final api = ApiClient(
      baseUri: Uri.parse('https://example.test'),
      tokenStorage: MemoryTokenStorage(),
      httpClient: MockClient((request) async {
        expect(request.url.path, '/api/locations/reverse');
        expect(request.url.queryParameters['latitude'], '6.927079');
        expect(request.url.queryParameters['longitude'], '79.861244');
        return http.Response(
          '{"displayName":"Colombo, Sri Lanka","source":"OpenStreetMap Nominatim"}',
          200,
        );
      }),
    );

    expect(
      await ApiLocationLookup(api).lookup(6.927079, 79.861244),
      'Colombo, Sri Lanka',
    );
  });

  test('profile registration and own profile update preserve shared auth and profile fields', () async {
    final api = ApiClient(
      baseUri: Uri.parse('https://example.test'),
      tokenStorage: MemoryTokenStorage()..token = 'jwt',
      httpClient: MockClient((request) async {
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['fullName'], 'Test Seller');
        expect(body['phoneNumber'], '0771234567');
        expect(body['address'], 'Colombo');
        expect(body.containsKey('id'), isFalse);
        final user = {
          'id': 's1',
          'email': 'seller@test.com',
          'roles': ['SELLER'],
          ...body,
        };
        if (request.method == 'PUT') {
          expect(request.url.path, '/api/auth/me');
          expect(request.headers['authorization'], 'Bearer jwt');
          return http.Response(jsonEncode(user), 200);
        }
        expect(request.url.path, '/api/auth/register');
        return http.Response(
          jsonEncode({
            'email': 'seller@test.com',
            'emailVerificationRequired': true,
          }),
          201,
        );
      }),
    );
    final repository = AuthRepository(apiClient: api, tokenStorage: MemoryTokenStorage());
    const profile = UserProfile(
      fullName: ' Test Seller ',
      phoneNumber: '0771234567',
      address: 'Colombo',
    );
    await repository.register(
      email: 'seller@test.com',
      password: 'Password123!',
      roles: [AppRole.seller],
      profile: profile,
    );
    final updated = await repository.updateProfile(profile);
    expect(updated.fullName, 'Test Seller');
    expect(updated.address, 'Colombo');
  });

  test('multipart photo upload sends bytes with bearer auth and returns a portable media path', () async {
    final api = ApiClient(
      baseUri: Uri.parse('https://example.test'),
      tokenStorage: MemoryTokenStorage()..token = 'seller-jwt',
      httpClient: MockClient((request) async {
        expect(request.url.path, '/api/material-photos');
        expect(request.headers['authorization'], 'Bearer seller-jwt');
        expect(
          request.headers['content-type'],
          startsWith('multipart/form-data;'),
        );
        expect(request.bodyBytes.length, greaterThan(photoBytes.length));
        return http.Response(
          '{"photoUrl":"/api/material-photos/owner/photo/png"}',
          201,
        );
      }),
    );
    expect(
      (await api.uploadPhoto(photoBytes))['photoUrl'],
      startsWith('/api/material-photos/'),
    );
  });

  testWidgets('profile fields require name phone and address', (tester) async {
    final fields = ProfileFieldsController();
    addTearDown(fields.dispose);
    final form = GlobalKey<FormState>();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Form(
            key: form,
            child: ProfileFields(controller: fields),
          ),
        ),
      ),
    );
    expect(form.currentState!.validate(), isFalse);
    await tester.pump();
    expect(find.text('Full name is required.'), findsOneWidget);
    expect(find.text('Enter a valid phone number.'), findsOneWidget);
    fields.name.text = 'A Buyer';
    fields.phone.text = '0771234567';
    fields.address.text = 'Colombo';
    expect(form.currentState!.validate(), isTrue);
  });

  testWidgets(
    'camera photo saves to gallery and retries failed upload without losing selection',
    (tester) async {
      tester.view.physicalSize = const Size(1100, 2600);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final gateway = MediaGateway();
      final media = FakeMedia();
      await tester.pumpWidget(
        MaterialApp(
          home: MaterialListingFormScreen(
            gateway: gateway,
            listingId: 'listing-1',
            media: media,
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('capture-material-photo')));
      await tester.pumpAndSettle();
      expect(media.gallerySaves, 1);
      expect(find.text('Saved to device gallery'), findsOneWidget);
      gateway.failUpload = true;
      await tester.tap(find.byKey(const Key('save-material')));
      await tester.pumpAndSettle();
      expect(find.text('Upload failed. Retry.'), findsOneWidget);
      expect(gateway.saved, isNull);
      gateway.failUpload = false;
      await tester.tap(find.byKey(const Key('save-material')));
      await tester.pumpAndSettle();
      expect(gateway.saved!.photoUrls, [
        '/api/material-photos/owner/photo/png',
      ]);
      final uploads = gateway.uploads;
      // The fake rejects listing save; retry must reuse the successful upload.
      await tester.tap(find.byKey(const Key('save-material')));
      await tester.pumpAndSettle();
      expect(gateway.uploads, uploads);
    },
  );

  testWidgets(
    'location shows address accuracy and recovery after lookup failure',
    (tester) async {
      final pending = Completer<String?>();
      var fail = true;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: LocationCard(
              latitude: 6.9,
              longitude: 79.8,
              accuracy: 8,
              lookup: (_, _) =>
                  fail ? pending.future : Future.value('Colombo, Sri Lanka'),
            ),
          ),
        ),
      );
      expect(find.text('Finding address...'), findsOneWidget);
      pending.completeError(StateError('Offline'));
      await tester.pumpAndSettle();
      expect(
        find.textContaining('Location captured, but address lookup is temporarily unavailable.'),
        findsOneWidget,
      );
      fail = false;
      await tester.tap(find.text('Retry address'));
      await tester.pumpAndSettle();
      expect(find.text('Colombo, Sri Lanka'), findsOneWidget);
      expect(find.text('GPS accuracy: approximately 8 m'), findsOneWidget);
    },
  );

  testWidgets(
    'back button reaches dashboard on direct entry without a history stack',
    (tester) async {
      final router = GoRouter(
        initialLocation: '/materials',
        routes: [
          GoRoute(
            path: '/home',
            builder: (_, _) => const Scaffold(body: Text('Dashboard')),
          ),
          GoRoute(
            path: '/materials',
            builder: (_, _) =>
                Scaffold(appBar: AppBar(leading: const DashboardBackButton())),
          ),
        ],
      );
      addTearDown(router.dispose);
      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(BackButton));
      await tester.pumpAndSettle();
      expect(find.text('Dashboard'), findsOneWidget);
    },
  );
}

class FakeMedia extends MaterialMedia {
  int gallerySaves = 0;
  @override
  Future<XFile?> takePhoto() async =>
      XFile.fromData(photoBytes, name: 'camera.png');
  @override
  Future<void> saveToGallery(Uint8List bytes) async {
    gallerySaves++;
  }
}

class MediaGateway extends FakeCategoryMaterials {
  bool failUpload = false;
  int uploads = 0;
  @override
  Future<String> uploadPhoto(List<int> bytes) async {
    uploads++;
    expect(bytes, photoBytes);
    if (failUpload) throw const ApiException('Upload failed. Retry.');
    return '/api/material-photos/owner/photo/png';
  }
}
