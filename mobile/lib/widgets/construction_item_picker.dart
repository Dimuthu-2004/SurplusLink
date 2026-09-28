import 'package:flutter/material.dart';
import 'package:mobile/materials/construction_item_template_models.dart';

class ConstructionItemPickerSheet extends StatefulWidget {
  const ConstructionItemPickerSheet({
    required this.templates,
    required this.onSelectTemplate,
    required this.onSelectCustom,
    super.key,
  });

  final List<ConstructionItemTemplate> templates;
  final ValueChanged<ConstructionItemTemplate> onSelectTemplate;
  final VoidCallback onSelectCustom;

  static Future<void> show({
    required BuildContext context,
    required List<ConstructionItemTemplate> templates,
    required ValueChanged<ConstructionItemTemplate> onSelectTemplate,
    required VoidCallback onSelectCustom,
  }) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => FractionallySizedBox(
        heightFactor: 0.85,
        child: ConstructionItemPickerSheet(
          templates: templates,
          onSelectTemplate: onSelectTemplate,
          onSelectCustom: onSelectCustom,
        ),
      ),
    );
  }

  @override
  State<ConstructionItemPickerSheet> createState() =>
      _ConstructionItemPickerSheetState();
}

class _ConstructionItemPickerSheetState
    extends State<ConstructionItemPickerSheet> {
  String _search = '';
  String _selectedClass = 'ALL';

  @override
  Widget build(BuildContext context) {
    final filtered = widget.templates.where((t) {
      if (!t.isActive) return false;
      if (_selectedClass != 'ALL' && t.itemClass != _selectedClass) {
        return false;
      }
      if (_search.trim().isEmpty) return true;
      final q = _search.toLowerCase();
      return t.name.toLowerCase().contains(q) ||
          t.categoryName.toLowerCase().contains(q);
    }).toList();

    return Padding(
      padding: EdgeInsets.only(
        top: 16,
        left: 16,
        right: 16,
        bottom: MediaQuery.of(context).viewInsets.bottom + 16,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'What are you listing?',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              IconButton(
                icon: const Icon(Icons.close),
                onPressed: () => Navigator.of(context).pop(),
              ),
            ],
          ),
          const SizedBox(height: 8),
          TextField(
            key: const Key('item-picker-search-field'),
            decoration: InputDecoration(
              hintText: 'Search items: Paint, Generator, Cement, Pipes...',
              prefixIcon: const Icon(Icons.search),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(horizontal: 12),
            ),
            onChanged: (val) => setState(() => _search = val),
          ),
          const SizedBox(height: 10),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _chip('ALL', 'All Items'),
                const SizedBox(width: 8),
                _chip('MATERIAL', 'Materials'),
                const SizedBox(width: 8),
                _chip('TOOL', 'Tools'),
                const SizedBox(width: 8),
                _chip('EQUIPMENT', 'Equipment'),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Expanded(
            child: filtered.isEmpty
                ? const Center(
                    child: Text('No construction items matching your search.'),
                  )
                : ListView.separated(
                    itemCount: filtered.length,
                    separatorBuilder: (context, index) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final template = filtered[index];
                      return ListTile(
                        key: Key('template-tile-${template.id}'),
                        title: Text(
                          template.name,
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                        subtitle: Text(
                          '${template.categoryName} • ${template.packageType != null ? "Sold in ${template.packageType}s (${template.baseUnit})" : "Sold per ${template.baseUnit}"}',
                        ),
                        trailing: Chip(
                          label: Text(
                            template.itemClass,
                            style: const TextStyle(fontSize: 10),
                          ),
                          visualDensity: VisualDensity.compact,
                        ),
                        onTap: () {
                          Navigator.of(context).pop();
                          widget.onSelectTemplate(template);
                        },
                      );
                    },
                  ),
          ),
          const Divider(),
          Card(
            color: Colors.orange.shade50,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
              side: BorderSide(color: Colors.orange.shade200),
            ),
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          "Can't find your construction item?",
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 13,
                          ),
                        ),
                        Text(
                          'List a custom item for manager verification.',
                          style: TextStyle(
                            fontSize: 11,
                            color: Colors.grey.shade700,
                          ),
                        ),
                      ],
                    ),
                  ),
                  OutlinedButton(
                    key: const Key('custom-item-fallback-button'),
                    onPressed: () {
                      Navigator.of(context).pop();
                      widget.onSelectCustom();
                    },
                    child: const Text('Add Custom'),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _chip(String id, String label) {
    final selected = _selectedClass == id;
    return ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => setState(() => _selectedClass = id),
    );
  }
}
