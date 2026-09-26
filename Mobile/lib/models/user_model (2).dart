class UserModel {
  final String? id;
  final String fullName;
  final String email;
  final String role;

  UserModel({
    this.id,
    required this.fullName,
    required this.email,
    required this.role,
  });

  factory UserModel.fromJson(
    Map<String, dynamic> json,
  ) {
    return UserModel(
      id: json['id']?.toString(),
      fullName: json['fullName'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? '',
    );
  }
}