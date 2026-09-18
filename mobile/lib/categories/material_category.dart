/// The category list's ID and display name; timestamps are not needed by forms.
class MaterialCategory {
  const MaterialCategory(this.id, this.name);
  final String id;
  final String name;

  factory MaterialCategory.fromJson(Map<String, dynamic> json) =>
      MaterialCategory(json['id'] as String, json['name'] as String);
}
