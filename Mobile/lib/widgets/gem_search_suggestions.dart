import 'package:flutter/material.dart';


class GemSearchSuggestion {
  final String label;
  final String detail;
  final String kind;

  const GemSearchSuggestion({
    required this.label,
    required this.detail,
    required this.kind,
  });
}

List<GemSearchSuggestion> buildGemSearchSuggestions(
  List<dynamic> gems,
  String query,
) {
  final needle = query.trim().toLowerCase();
  if (needle.isEmpty) return const [];

  final listings =
      gems
          .where((gem) => gem.title.toLowerCase().contains(needle))
          .map(
            (gem) => GemSearchSuggestion(
              label: gem.title,
              detail: [
                gem.gemType,
                gem.color,
                '${gem.caratWeight.toStringAsFixed(2)} ct',
              ].where((part) => part.isNotEmpty).join(' · '),
              kind: 'Gemstone',
            ),
          )
          .toList()
        ..sort((a, b) => _compareSuggestions(a, b, needle));

  final facets = <String, GemSearchSuggestion>{};
  void addFacet(String label, String kind) {
    final clean = label.trim();
    if (clean.isEmpty || !clean.toLowerCase().contains(needle)) return;
    final key = '$kind-${clean.toLowerCase()}';
    facets.putIfAbsent(
      key,
      () => GemSearchSuggestion(
        label: clean,
        detail: 'Explore ${kind.toLowerCase()} matches',
        kind: kind,
      ),
    );
  }

  for (final gem in gems) {
    addFacet(gem.gemType, 'Type');
    addFacet(gem.color, 'Colour');
    addFacet(gem.cut, 'Cut');
    addFacet(gem.sellerName, 'Seller');
  }
  final rankedFacets = facets.values.toList()
    ..sort((a, b) => _compareSuggestions(a, b, needle));

  return [...listings.take(2), ...rankedFacets.take(2)];
}

int _compareSuggestions(
  GemSearchSuggestion a,
  GemSearchSuggestion b,
  String needle,
) {
  final aStarts = a.label.toLowerCase().startsWith(needle);
  final bStarts = b.label.toLowerCase().startsWith(needle);
  if (aStarts != bStarts) return aStarts ? -1 : 1;
  return a.label.toLowerCase().compareTo(b.label.toLowerCase());
}

class GemSearchSuggestions extends StatelessWidget {
  final List<dynamic> gems;
  final String query;
  final ValueChanged<String> onSelected;

  const GemSearchSuggestions({
    super.key,
    required this.gems,
    required this.query,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    final cleanQuery = query.trim();
    if (cleanQuery.isEmpty) return const SizedBox.shrink();
    final suggestions = buildGemSearchSuggestions(gems, cleanQuery);

    return Semantics(
      label: 'Gemstone suggestions',
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
                    ? 'EXPLORE THE COLLECTION'
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
                onTap: () => onSelected(suggestion.label),
              ),
            const Divider(height: 13, color: Color(0xFFE7DFD2)),
            InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: () => onSelected(cleanQuery),
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
                        'Search all for “$cleanQuery”',
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
    );
  }
}

class _SuggestionRow extends StatelessWidget {
  final GemSearchSuggestion suggestion;
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
            const _SuggestionIcon(icon: Icons.diamond_outlined),
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
