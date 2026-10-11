import 'package:flutter/material.dart';

/// Centralized Gemora design tokens and brand colors.
/// Reuses exact palette from Login, Home, and Seller Dashboard:
/// - Dark green: #08251E
/// - Green accent: #16483B
/// - Gold: #E4BC74 / Dark Gold: #C99242
/// - Warm cream / off-white: #FAF7F0
/// - Border: #E7DFD2
/// - Muted text: #697771
class GemoraColors {
  GemoraColors._();

  // Core Brand Palette
  static const Color darkGreen = Color(0xFF08251E);
  static const Color green = Color(0xFF16483B);
  static const Color deepGreen = Color(0xFF16483B);
  static const Color gold = Color(0xFFE4BC74);
  static const Color darkGold = Color(0xFFC99242);
  static const Color cream = Color(0xFFFAF7F0);
  static const Color white = Color(0xFFFFFFFF);
  static const Color mutedText = Color(0xFF697771);
  static const Color border = Color(0xFFE7DFD2);
  static const Color subtleSurface = Color(0xFFF7F4EB);

  // Status Badge Colors (Cohesive, strictly NO purple)
  static const Color statusDraft = Color(0xFF5A6B64);
  static const Color statusSubmitted = Color(0xFFC99242);
  static const Color statusUnderReview = Color(0xFF16483B);
  static const Color statusOfficerReview = Color(0xFF08251E);
  static const Color statusApproved = Color(0xFF2E7D32);
  static const Color statusRevisionRequired = Color(0xFFD97706);
  static const Color statusRejected = Color(0xFFC62828);
  static const Color statusNotRequired = Color(0xFF697771);

  static Color getStatusColor(String? status) {
    switch (status?.toLowerCase()) {
      case 'draft':
        return statusDraft;
      case 'submitted':
        return statusSubmitted;
      case 'undercompliancereview':
        return statusUnderReview;
      case 'underofficerreview':
        return statusOfficerReview;
      case 'approved':
        return statusApproved;
      case 'revisionrequired':
        return statusRevisionRequired;
      case 'rejected':
        return statusRejected;
      case 'notrequired':
        return statusNotRequired;
      default:
        return mutedText;
    }
  }

  static String getStatusLabel(String? status) {
    if (status == null || status.isEmpty) return 'Draft';
    switch (status.toLowerCase()) {
      case 'draft':
        return 'Draft';
      case 'submitted':
        return 'Submitted';
      case 'undercompliancereview':
        return 'Compliance Review';
      case 'underofficerreview':
        return 'Officer Review';
      case 'approved':
        return 'Approved';
      case 'revisionrequired':
        return 'Revision Required';
      case 'rejected':
        return 'Rejected';
      case 'notrequired':
        return 'Not Required';
      default:
        return status;
    }
  }
}
