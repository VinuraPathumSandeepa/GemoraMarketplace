import 'package:flutter/material.dart';

import '../models/gem_listing_model.dart';

class SellerListingSuggestion {
  final String label;
  final String detail;
  final String kind;

  const SellerListingSuggestion({
    required this.label,
    required this.detail,
    required this.kind,
  });
}

bool matchesSellerListing(GemListingModel listing, String query) {
  final needle = query.trim().toLowerCase();
  if (needle.isEmpty) return true;
  return [
    listing.title,
    listing.gemType,
    listing.color,
    listing.clarity,
    listing.cut,
    listing.certificateNumber,
    listing.certificateAuthority,
  ].any((value) => value?.toLowerCase().contains(needle) ?? false);
}

List<SellerListingSuggestion> buildSellerListingSuggestions(
  List<GemListingModel> listings,
  String query,
) {
  final needle = query.trim().toLowerCase();
  if (needle.isEmpty) return const [];

  final titles =
      listings
          .where((listing) => listing.title.toLowerCase().contains(needle))
          .map(
            (listing) => SellerListingSuggestion(
              label: listing.title,
              detail: [
                listing.gemType,
                listing.color,
                _statusLabel(listing.status),
              ].where((part) => part.isNotEmpty).join(' · '),
              kind: 'Listing',
            ),
          )
          .toList()
        ..sort((a, b) => _compare(a, b, needle));

  final facets = <String, SellerListingSuggestion>{};
  void addFacet(String? label, String kind) {
    final clean = label?.trim() ?? '';
    if (clean.isEmpty || !clean.toLowerCase().contains(needle)) return;
    final key = '$kind-${clean.toLowerCase()}';
    facets.putIfAbsent(
      key,
      () => SellerListingSuggestion(
        label: clean,
        detail: 'Search ${kind.toLowerCase()} in your listings',
        kind: kind,
      ),
    );
  }

  for (final listing in listings) {
    addFacet(listing.gemType, 'Type');
    addFacet(listing.color, 'Colour');
    addFacet(listing.cut, 'Cut');
    addFacet(listing.clarity, 'Clarity');
    addFacet(listing.certificateNumber, 'Certificate');
    addFacet(listing.certificateAuthority, 'Authority');
  }
  final rankedFacets = facets.values.toList()
    ..sort((a, b) => _compare(a, b, needle));

  return [...titles.take(2), ...rankedFacets.take(2)];
}

int _compare(
  SellerListingSuggestion a,
  SellerListingSuggestion b,
  String needle,
) {
  final aStarts = a.label.toLowerCase().startsWith(needle);
  final bStarts = b.label.toLowerCase().startsWith(needle);
  if (aStarts != bStarts) return aStarts ? -1 : 1;
  return a.label.toLowerCase().compareTo(b.label.toLowerCase());
}

String _statusLabel(String status) {
  switch (status.toLowerCase()) {
    case 'pendingverification':
      return 'Pending review';
    case 'changesrequested':
      return 'Changes requested';
    default:
      return status;
  }
}

class SellerListingSearch extends StatefulWidget {
  final List<GemListingModel> listings;
  final String initialQuery;
  final ValueChanged<String> onQueryChanged;

  const SellerListingSearch({
    super.key,
    required this.listings,
    this.initialQuery = '',
    required this.onQueryChanged,
  });

  @override
  State<SellerListingSearch> createState() => _SellerListingSearchState();
}

class _SellerListingSearchState extends State<SellerListingSearch> {
  late final TextEditingController _controller;
  final _focusNode = FocusNode();

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.initialQuery);
    _focusNode.addListener(_refresh);
  }

  void _refresh() {
    if (mounted) setState(() {});
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  void _select(String value) {
    _controller.value = TextEditingValue(
      text: value,
      selection: TextSelection.collapsed(offset: value.length),
    );
    widget.onQueryChanged(value);
    _focusNode.unfocus();
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final query = _controller.text.trim();
    final suggestions = buildSellerListingSuggestions(widget.listings, query);

    return TextFieldTapRegion(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextField(
            controller: _controller,
            focusNode: _focusNode,
            textInputAction: TextInputAction.search,
            onChanged: (value) {
              setState(() {});
              widget.onQueryChanged(value);
            },
            onSubmitted: (_) => _focusNode.unfocus(),
            decoration: InputDecoration(
              hintText: 'Search title, type, colour or certificate...',
              prefixIcon: const Icon(Icons.search_rounded),
              suffixIcon: query.isEmpty
                  ? null
                  : IconButton(
                      tooltip: 'Clear search',
                      onPressed: () => _select(''),
                      icon: const Icon(Icons.close_rounded),
                    ),
              filled: true,
              fillColor: Colors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: const BorderSide(color: Color(0xFFE7DFD2)),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: const BorderSide(color: Color(0xFFE7DFD2)),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: const BorderSide(
                  color: Color(0xFFC99242),
                  width: 1.5,
                ),
              ),
            ),
          ),
          if (_focusNode.hasFocus && query.isNotEmpty)
            Semantics(
              label: 'Listing suggestions',
              child: Container(
                margin: const EdgeInsets.only(top: 8),
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(color: const Color(0xFFE7DFD2)),
                  boxShadow: const [
                    BoxShadow(
                      color: Color(0x1C08251E),
                      blurRadius: 28,
                      offset: Offset(0, 12),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(11, 8, 11, 7),
                      child: Text(
                        suggestions.isEmpty
                            ? 'SEARCH YOUR LISTINGS'
                            : 'SUGGESTED SEARCHES',
                        style: const TextStyle(
                          color: Color(0xFF697771),
                          fontSize: 10,
                          letterSpacing: 1.3,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    for (final suggestion in suggestions)
                      _SuggestionRow(
                        suggestion: suggestion,
                        onTap: () => _select(suggestion.label),
                      ),
                    const Divider(height: 13, color: Color(0xFFE7DFD2)),
                    InkWell(
                      borderRadius: BorderRadius.circular(12),
                      onTap: () => _select(query),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 10,
                        ),
                        child: Row(
                          children: [
                            const _SuggestionIcon(icon: Icons.search_rounded),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                'Search all for “$query”',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  color: Color(0xFF10241F),
                                  fontSize: 13,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                            const Icon(
                              Icons.arrow_outward_rounded,
                              size: 17,
                              color: Color(0xFFC99242),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _SuggestionRow extends StatelessWidget {
  final SellerListingSuggestion suggestion;
  final VoidCallback onTap;

  const _SuggestionRow({required this.suggestion, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(12),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        child: Row(
          children: [
            _SuggestionIcon(
              icon:
                  suggestion.kind == 'Certificate' ||
                      suggestion.kind == 'Authority'
                  ? Icons.description_outlined
                  : Icons.diamond_outlined,
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    suggestion.label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Color(0xFF10241F),
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    suggestion.detail,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Color(0xFF697771),
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Text(
              suggestion.kind,
              style: const TextStyle(
                color: Color(0xFF16483B),
                fontSize: 10,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SuggestionIcon extends StatelessWidget {
  final IconData icon;

  const _SuggestionIcon({required this.icon});

  @override
  Widget build(BuildContext context) => Container(
    width: 34,
    height: 34,
    decoration: BoxDecoration(
      color: const Color(0xFFF3F4EF),
      borderRadius: BorderRadius.circular(10),
    ),
    child: Icon(icon, size: 18, color: const Color(0xFF16483B)),
  );
}
