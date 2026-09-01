# 🏫 School Management System

[![Live Demo](https://img.shields.io/badge/Live-Demo-green)](https://schoolms-mfundisomzobe-b4eggnbmb6eyh0gb.centralus-01.azurewebsites.net/)
[![Deployed on Azure](https://img.shields.io/badge/Deployed-Azure-0078D4)](https://azure.microsoft.com/)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive, multi-role School Management System built with ASP.NET Core MVC and deployed on Microsoft Azure.

## 🚀 Live Demo

**[View Live Application](https://schoolms-mfundisomzobe-b4eggnbmb6eyh0gb.centralus-01.azurewebsites.net/)**

## 📋 Table of Contents

- [Features](#-features)
- [User Roles](#-user-roles)
- [Technologies Used](#-technologies-used)
- [Architecture](#-architecture)
- [Security](#-security)
- [Getting Started](#-getting-started)
- [Database Schema](#-database-schema)
- [Deployment](#-deployment)
- [Project Structure](#-project-structure)
- [Screenshots](#-screenshots)
- [Future Enhancements](#-future-enhancements)
- [Contributing](#-contributing)
- [License](#-license)
- [Contact](#-contact)

## ✨ Features

### 🔐 Authentication & Authorization
- Cookie-based authentication with ASP.NET Core Identity
- Role-based authorization (Admin, Teacher, Student, Parent)
- Secure registration workflow (no public sign-up)
- Email confirmation required before login
- Account lockout after 5 failed attempts
- "Remember Me" functionality

### 👨‍🏫 Admin Features
- Full CRUD operations for Students, Teachers, Courses, and Classes
- Student enrollment management with duplicate prevention
- Class capacity enforcement
- Bulk student import via CSV
- Teacher invitation system with secure tokens
- Comprehensive dashboard with analytics

### 👩‍🏫 Teacher Features
- View assigned class rosters
- Mark attendance (Present, Absent, Late, Excused)
- Enter grades with automatic letter grade calculation
- Grade brackets: A (90-100), B (80-89), C (70-79), D (60-69), F (<60)
- View class performance statistics

### 👨‍🎓 Student Features
- View personal class schedule
- View grades and academic progress
- View attendance history with percentages
- First-login password reset enforcement

### 👪 Parent Features
- View linked child's grades
- Monitor attendance records
- Multiple children support
- Secure access code registration

### 📊 Dashboard Features
- Admin: Real-time statistics and charts
- Teacher: Class-specific performance metrics
- Student: Personalized academic summary
- Parent: Child progress overview

## 👥 User Roles

| Role | Permissions | Restrictions |
|------|-------------|--------------|
| **Admin** | Full system control, user management, enrollment, bulk import | Cannot mark attendance/grades |
| **Teacher** | Class management, attendance, grading | Only assigned classes, no user management |
| **Student** | View personal data (schedule, grades, attendance) | Read-only, cannot view other students |
| **Parent** | View linked children's data | Read-only, cannot edit any records |

## 🛠️ Technologies Used

### Backend
- **ASP.NET Core 9.0** - Web framework
- **Entity Framework Core 9.0** - ORM
- **ASP.NET Core Identity** - Authentication & Authorization
- **SQL Server** - Database
- **Azure SQL** - Production database

### Frontend
- **Razor Views** - Server-side rendering
- **Bootstrap 5** - UI framework
- **Custom CSS** - Modern responsive design

### Cloud & DevOps
- **Azure App Service** - Hosting
- **Azure SQL Database** - Production database
- **GitHub Actions** - CI/CD pipeline

### Security
- **ASP.NET Core Identity** - Authentication
- **Data Protection API** - Token encryption
- **PBKDF2** - Password hashing (via Identity)
- **Anti-Forgery Tokens** - CSRF protection

## 🏗️ Architecture

The application follows a clean, structured architecture:
