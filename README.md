# 🏢 Smart Attendance API — Backend

> REST API untuk Sistem Absensi Karyawan Digital, dibangun dengan **ASP.NET Core 9** dan **MySQL**.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![Railway](https://img.shields.io/badge/Deploy-Railway-0B0D0E?style=for-the-badge&logo=railway&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

---

## 📋 Daftar Isi

- [Tentang Project](#-tentang-project)
- [Fitur](#-fitur)
- [Tech Stack](#-tech-stack)
- [Prasyarat](#-prasyarat)
- [Instalasi & Setup](#-instalasi--setup)
- [Menjalankan Aplikasi](#-menjalankan-aplikasi)
- [API Endpoints](#-api-endpoints)
- [Struktur Project](#-struktur-project)
- [Deployment](#-deployment)
- [Akun Default](#-akun-default)
- [Frontend Repository](#-frontend-repository)

---

## 📖 Tentang Project

**Smart Attendance API** adalah backend REST API untuk sistem absensi karyawan digital. Sistem ini mendukung fitur check-in/check-out berbasis lokasi GPS, manajemen cuti, dan dashboard admin.

---

## ✨ Fitur

- 🔐 **Autentikasi JWT** — Login & register dengan token JWT
- 📍 **Absensi Berbasis GPS** — Check-in/check-out dengan validasi lokasi & radius kantor
- 📸 **Foto Selfie** — Upload foto saat check-in & check-out
- 📅 **Manajemen Cuti** — Pengajuan & approval cuti oleh admin
- 👥 **Manajemen User** — CRUD user oleh admin
- ⚙️ **Pengaturan Sistem** — Konfigurasi jam kerja, lokasi kantor, radius, hari kerja
- 📊 **Dashboard** — Statistik dan ringkasan absensi
- 🌱 **Auto Seed** — Database otomatis membuat tabel & data default saat pertama kali dijalankan

---

## 🛠 Tech Stack

| Teknologi | Versi | Keterangan |
|-----------|-------|------------|
| .NET | 9.0 | Framework utama |
| ASP.NET Core | 9.0 | Web API |
| Entity Framework Core | 9.0 | ORM Database |
| Pomelo.EntityFrameworkCore.MySql | 9.0 | Provider MySQL untuk EF Core |
| BCrypt.Net | 4.1.0 | Hashing password |
| JWT Bearer | 9.0 | Autentikasi token |
| MySQL | 8.0+ | Database |

---

## 📦 Prasyarat

Pastikan sudah terinstall di komputer kamu:

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** — Cek dengan:
   ```bash
   dotnet --version
   ```
2. **[MySQL 8.0+](https://dev.mysql.com/downloads/)** — Bisa menggunakan XAMPP, Laragon, atau MySQL Server langsung
3. **Git** — Untuk clone repository

---

## 🚀 Instalasi & Setup

### 1. Clone Repository

```bash
git clone https://github.com/aldievanz/be_absensi.git
cd be_absensi
```

### 2. Buat Database MySQL

Buka MySQL (melalui phpMyAdmin, MySQL Workbench, atau terminal):

```sql
CREATE DATABASE smart_attendance;
```

> **💡 Opsional:** Kamu juga bisa mengimport file `database/smart_attendance.sql` untuk membuat tabel beserta data sample.
>
> ```bash
> mysql -u root -p smart_attendance < database/smart_attendance.sql
> ```

### 3. Konfigurasi Connection String

Edit file `appsettings.json` dan sesuaikan pengaturan database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=smart_attendance;User=root;Password=;"
  },
  "Jwt": {
    "Key": "SmartAttendance2026SuperSecretKeyYangSangatPanjangDanAman!@#$",
    "Issuer": "SmartAttendanceApi",
    "Audience": "SmartAttendanceFE"
  }
}
```

> **⚠️ Sesuaikan** `User` dan `Password` dengan akun MySQL kamu. Jika menggunakan XAMPP, biasanya `User=root` dan `Password=` (kosong).

### 4. Restore Dependencies

```bash
dotnet restore
```

---

## ▶️ Menjalankan Aplikasi

```bash
dotnet run
```

API akan berjalan di: **`http://localhost:5210`**

> **📝 Catatan:** Saat pertama kali dijalankan, aplikasi akan otomatis:
> - Membuat tabel-tabel di database (jika belum ada)
> - Menambahkan akun admin & user default
> - Menambahkan pengaturan default (jam kerja, lokasi kantor, dll.)

---

## 📡 API Endpoints

### Auth
| Method | Endpoint | Keterangan |
|--------|----------|------------|
| `POST` | `/api/auth/login` | Login user |
| `POST` | `/api/auth/register` | Register user baru |
| `GET` | `/api/auth/me` | Get profil user yang login |

### Attendance (Absensi)
| Method | Endpoint | Keterangan |
|--------|----------|------------|
| `POST` | `/api/attendance/checkin` | Check-in absensi |
| `POST` | `/api/attendance/checkout` | Check-out absensi |
| `GET` | `/api/attendance/today` | Status absensi hari ini |
| `GET` | `/api/attendance/history` | Riwayat absensi user |
| `GET` | `/api/attendance/all` | Semua data absensi (Admin) |
| `GET` | `/api/attendance/report` | Laporan absensi (Admin) |

### Leave (Cuti/Izin)
| Method | Endpoint | Keterangan |
|--------|----------|------------|
| `POST` | `/api/leave` | Ajukan cuti/izin |
| `GET` | `/api/leave` | List cuti user |
| `GET` | `/api/leave/all` | Semua data cuti (Admin) |
| `PUT` | `/api/leave/{id}/approve` | Approve cuti (Admin) |
| `PUT` | `/api/leave/{id}/reject` | Reject cuti (Admin) |

### User Management
| Method | Endpoint | Keterangan |
|--------|----------|------------|
| `GET` | `/api/users` | List semua user (Admin) |
| `POST` | `/api/users` | Tambah user baru (Admin) |
| `PUT` | `/api/users/{id}` | Update user (Admin) |
| `DELETE` | `/api/users/{id}` | Hapus user (Admin) |

### Settings (Pengaturan)
| Method | Endpoint | Keterangan |
|--------|----------|------------|
| `GET` | `/api/settings` | Get semua pengaturan |
| `PUT` | `/api/settings` | Update pengaturan (Admin) |

### Dashboard
| Method | Endpoint | Keterangan |
|--------|----------|------------|
| `GET` | `/api/dashboard` | Data dashboard |

---

## 📁 Struktur Project

```
be_absensi/
├── Controllers/             # API Controllers
│   ├── AttendanceController.cs   # Endpoint absensi
│   ├── AuthController.cs         # Endpoint autentikasi
│   ├── DashboardController.cs    # Endpoint dashboard
│   ├── LeaveController.cs        # Endpoint cuti/izin
│   ├── SettingsController.cs     # Endpoint pengaturan
│   └── UserController.cs         # Endpoint manajemen user
├── Data/                    # Database context (EF Core)
├── DTOs/                    # Data Transfer Objects
├── Models/                  # Entity models
│   ├── AppSetting.cs
│   ├── Attendance.cs
│   ├── LeaveRequest.cs
│   └── User.cs
├── Properties/              # Launch settings
├── database/
│   └── smart_attendance.sql # SQL schema + data sample
├── appsettings.json         # Konfigurasi aplikasi
├── Program.cs               # Entry point & konfigurasi
├── Procfile                 # Deploy ke Railway
└── SmartAttendanceApi.csproj # Project file
```

---

## ☁️ Deployment

### Deploy ke Railway

1. Buat akun di [Railway](https://railway.app/)
2. Buat project baru → **Deploy from GitHub repo**
3. Tambahkan **MySQL** service di Railway
4. Set environment variable:
   ```
   MYSQL_URL=mysql://user:password@host:port/database
   ```
   > Railway akan otomatis menyediakan `MYSQL_URL` jika kamu menambahkan MySQL service.
5. Railway akan otomatis build & deploy

---

## 🔑 Akun Default

Setelah aplikasi berjalan pertama kali, akun berikut akan otomatis dibuat:

| Role | Email | Password |
|------|-------|----------|
| **Admin** | `admin@example.com` | `password123` |
| **User** | `user@example.com` | `password123` |

> **⚠️ Penting:** Ganti password default ini setelah login pertama kali!

---

## 🖥 Frontend Repository

Frontend untuk project ini tersedia di repository terpisah:

👉 **[fe_absensi (Frontend)](https://github.com/aldievanz/fe_absensi)**

---

## 📄 Lisensi

Project ini dibuat untuk keperluan belajar dan pengembangan.

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/aldievanz">aldievanz</a>
</p>
