import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_si.dart';
import 'app_localizations_ta.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('si'),
    Locale('ta'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'SurplusLink'**
  String get appTitle;

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get language;

  /// No description provided for @english.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get english;

  /// No description provided for @sinhala.
  ///
  /// In en, this message translates to:
  /// **'Sinhala'**
  String get sinhala;

  /// No description provided for @tamil.
  ///
  /// In en, this message translates to:
  /// **'Tamil'**
  String get tamil;

  /// No description provided for @createRequirement.
  ///
  /// In en, this message translates to:
  /// **'Create Requirement'**
  String get createRequirement;

  /// No description provided for @whatDoYouNeed.
  ///
  /// In en, this message translates to:
  /// **'What do you need?'**
  String get whatDoYouNeed;

  /// No description provided for @searchConstructionItems.
  ///
  /// In en, this message translates to:
  /// **'Search construction items...'**
  String get searchConstructionItems;

  /// No description provided for @category.
  ///
  /// In en, this message translates to:
  /// **'Category'**
  String get category;

  /// No description provided for @preferencesOptional.
  ///
  /// In en, this message translates to:
  /// **'Preferences (optional)'**
  String get preferencesOptional;

  /// No description provided for @saveDraft.
  ///
  /// In en, this message translates to:
  /// **'Save draft'**
  String get saveDraft;

  /// No description provided for @validationSummary.
  ///
  /// In en, this message translates to:
  /// **'Please correct the highlighted fields.'**
  String get validationSummary;

  /// No description provided for @submitListing.
  ///
  /// In en, this message translates to:
  /// **'Submit Listing'**
  String get submitListing;

  /// No description provided for @packageType.
  ///
  /// In en, this message translates to:
  /// **'Package type'**
  String get packageType;

  /// No description provided for @packages.
  ///
  /// In en, this message translates to:
  /// **'Packages'**
  String get packages;

  /// No description provided for @customPackageName.
  ///
  /// In en, this message translates to:
  /// **'Custom package name'**
  String get customPackageName;

  /// No description provided for @baseMeasurement.
  ///
  /// In en, this message translates to:
  /// **'Base measurement'**
  String get baseMeasurement;

  /// No description provided for @piecesUnits.
  ///
  /// In en, this message translates to:
  /// **'Pieces / Units'**
  String get piecesUnits;

  /// No description provided for @bulkQuantity.
  ///
  /// In en, this message translates to:
  /// **'Bulk Quantity'**
  String get bulkQuantity;

  /// No description provided for @howSold.
  ///
  /// In en, this message translates to:
  /// **'How is this item sold?'**
  String get howSold;

  /// No description provided for @requiredField.
  ///
  /// In en, this message translates to:
  /// **'This field is required.'**
  String get requiredField;

  /// No description provided for @packageSizeRequired.
  ///
  /// In en, this message translates to:
  /// **'Choose or enter the amount contained in one package.'**
  String get packageSizeRequired;

  /// No description provided for @invalidMeasurement.
  ///
  /// In en, this message translates to:
  /// **'Choose a valid measurement unit.'**
  String get invalidMeasurement;

  /// No description provided for @wholeNumberRequired.
  ///
  /// In en, this message translates to:
  /// **'Enter a whole number greater than zero.'**
  String get wholeNumberRequired;

  /// No description provided for @futureDateRequired.
  ///
  /// In en, this message translates to:
  /// **'Choose a future date.'**
  String get futureDateRequired;

  /// No description provided for @anyPreference.
  ///
  /// In en, this message translates to:
  /// **'Any / No preference'**
  String get anyPreference;

  /// No description provided for @draftSaved.
  ///
  /// In en, this message translates to:
  /// **'Draft saved'**
  String get draftSaved;

  /// No description provided for @listingSubmitted.
  ///
  /// In en, this message translates to:
  /// **'Listing submitted'**
  String get listingSubmitted;

  /// No description provided for @listingSubmittedMessage.
  ///
  /// In en, this message translates to:
  /// **'Your material was sent for manager review.'**
  String get listingSubmittedMessage;

  /// No description provided for @listingResubmitted.
  ///
  /// In en, this message translates to:
  /// **'Listing resubmitted'**
  String get listingResubmitted;

  /// No description provided for @listingResubmittedMessage.
  ///
  /// In en, this message translates to:
  /// **'Your updated listing was sent for manager review.'**
  String get listingResubmittedMessage;

  /// No description provided for @requirementSubmitted.
  ///
  /// In en, this message translates to:
  /// **'Requirement submitted'**
  String get requirementSubmitted;

  /// No description provided for @requirementSubmittedMessage.
  ///
  /// In en, this message translates to:
  /// **'Your requirement is ready for matching.'**
  String get requirementSubmittedMessage;

  /// No description provided for @selectionSubmitted.
  ///
  /// In en, this message translates to:
  /// **'Selection submitted'**
  String get selectionSubmitted;

  /// No description provided for @selectionSubmittedMessage.
  ///
  /// In en, this message translates to:
  /// **'Your selected sellers were sent for manager approval.'**
  String get selectionSubmittedMessage;

  /// No description provided for @findingSuitableMatches.
  ///
  /// In en, this message translates to:
  /// **'Finding suitable matches...'**
  String get findingSuitableMatches;

  /// No description provided for @matchingWorkflowMessage.
  ///
  /// In en, this message translates to:
  /// **'Our matching workflow is checking available sellers.'**
  String get matchingWorkflowMessage;

  /// No description provided for @stillWorkingOnMatches.
  ///
  /// In en, this message translates to:
  /// **'Still working on your matches...'**
  String get stillWorkingOnMatches;

  /// No description provided for @matchingCouldNotComplete.
  ///
  /// In en, this message translates to:
  /// **'Matching could not be completed.'**
  String get matchingCouldNotComplete;

  /// No description provided for @retryMatching.
  ///
  /// In en, this message translates to:
  /// **'Retry Matching'**
  String get retryMatching;

  /// No description provided for @workflowRunning.
  ///
  /// In en, this message translates to:
  /// **'Matching is running. Status updates automatically.'**
  String get workflowRunning;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'si', 'ta'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'si':
      return AppLocalizationsSi();
    case 'ta':
      return AppLocalizationsTa();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
