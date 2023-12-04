﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace studentManagement {
    /// <summary>
    ///   Sqlite connector (wrapper for SQLiteConnection).
    ///   Only for internal use, do not use outside this file.
    /// </summary>
    internal class SqliteConnector {
        private readonly SQLiteConnection _connection;

        public SqliteConnector(string connectionString) {
            _connection = new SQLiteConnection(connectionString);
            _connection.Open();
        }

        public SQLiteCommand createAndExecuteCommand(string commandText, params string[] args) {
            var command = _connection.CreateCommand();
            command.CommandText = commandText;
            var parameters = Regex.Match(commandText, @"@\w+");
            var i = 0;
            while (parameters.Success) {
                command.Parameters.AddWithValue(parameters.Value, args[i++]);
                parameters = parameters.NextMatch();
            }

            command.ExecuteNonQuery();
            return command;
        }
    }

    public class Database {
        private readonly SqliteConnector _connector;

        private static string _hashPassword(string password) {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = new SHA256Managed().ComputeHash(passwordBytes);
            password = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return password;
        }

        /// <summary>
        ///   Create database and tables if not exists.
        /// </summary>
        public Database(string connectionString) {
            _connector = new SqliteConnector(connectionString);
            _connector.createAndExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS DSSinhVien (
                    MaSinhVien TEXT PRIMARY KEY,
                    HoTen TEXT,
                    NgaySinh TEXT,
                    GioiTinh TEXT,
                    MaKhoa TEXT REFERENCES DSKhoa(MaKhoa),
                    MaLop TEXT REFERENCES DSLop(MaLop),
                    LopTruong INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS DSKhoa (
                    MaKhoa TEXT PRIMARY KEY,
                    TenKhoa TEXT
                );
                CREATE TABLE IF NOT EXISTS DSLop (
                    MaLop TEXT PRIMARY KEY,
                    TenLop TEXT,
                    MaKhoa TEXT REFERENCES DSKhoa(MaKhoa)
                );
                CREATE TABLE IF NOT EXISTS DSMonHoc (
                    MaMonHoc TEXT PRIMARY KEY,
                    TenMonHoc TEXT
                );
                CREATE TABLE IF NOT EXISTS DSLopHocPhan (
                    MaLopHocPhan TEXT PRIMARY KEY,
                    TenLopHocPhan TEXT,
                    MaKhoa TEXT REFERENCES DSKhoa(MaKhoa),
                    MaMonHoc TEXT REFERENCES DSMonHoc(MaMonHoc)
                );
                CREATE TABLE IF NOT EXISTS DSSinhVienLopHocPhan (
                    MaSinhVien TEXT REFERENCES DSSinhVien(MaSinhVien),
                    MaLopHocPhan TEXT REFERENCES DSLopHocPhan(MaLopHocPhan),
                    PRIMARY KEY (MaSinhVien, MaLopHocPhan)
                );
                CREATE TABLE IF NOT EXISTS DSDiem (
                    MaSinhVien TEXT REFERENCES DSSinhVien(MaSinhVien),
                    MaMonHoc TEXT REFERENCES DSMonHoc(MaMonHoc),
                    Diem INTEGER,
                    PRIMARY KEY (MaSinhVien, MaMonHoc)
                );
                CREATE TABLE IF NOT EXISTS DSUser (
                    Username TEXT PRIMARY KEY,
                    Password TEXT,
                    MaSinhVien TEXT REFERENCES DSSinhVien(MaSinhVien)
                );
            ");
        }

        /// <summary>
        ///   Insert student.
        ///   If student exists, update student info
        /// </summary>
        /// <param name="maSinhVien"></param>
        /// <param name="hoTen"></param>
        /// <param name="ngaySinh"></param>
        /// <param name="gioiTinh"></param>
        /// <param name="maKhoa"></param>
        /// <param name="maLop"></param>
        /// <param name="lopTruong"></param>
        /// <returns> true if insert success </returns>
        public bool insertStudent(
            string maSinhVien,
            string hoTen,
            string ngaySinh,
            string gioiTinh,
            string maKhoa,
            string maLop,
            bool lopTruong = false
        ) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSSinhVien (MaSinhVien, HoTen, NgaySinh, GioiTinh, MaKhoa, MaLop, LopTruong)
                    VALUES (@MaSinhVien, @hoTen, @ngaySinh, @gioiTinh, @maKhoa, @maLop, @lopTruong)
                ", maSinhVien, hoTen, ngaySinh, gioiTinh, maKhoa, maLop, lopTruong ? "1" : "0");
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSUser (Username, Password, MaSinhVien)
                    VALUES (@MaSinhVien, @password, @MaSinhVien)
                ", maSinhVien, _hashPassword(maSinhVien), maSinhVien);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Update student.
        /// </summary>
        /// <param name="maSinhVien"></param>
        /// <returns> true if success whether student exists or not </returns>
        public bool removeStudent(string maSinhVien) {
            try {
                _connector.createAndExecuteCommand(@"
                    DELETE FROM DSSinhVien WHERE MaSinhVien = @MaSinhVien
                ", maSinhVien);
                _connector.createAndExecuteCommand(@"
                    DELETE FROM DSUser WHERE MaSinhVien = @MaSinhVien
                ", maSinhVien);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Check login.
        /// </summary>
        /// <returns> true if username and password is correct </returns>
        public bool checkLogin(string username, string password) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT Password FROM DSUser WHERE Username = @username
            ", username);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return false;
            }

            var hashPassword = reader.GetString(0);
            return hashPassword == _hashPassword(password);
        }

        /// <summary>
        ///   Update password.
        /// </summary>
        /// <returns> true if username exists </returns>
        public bool updatePassword(string username, string password) {
            var command = _connector.createAndExecuteCommand(@"
                UPDATE DSUser SET Password = @password WHERE Username = @username
            ", _hashPassword(password), username);
            return command.ExecuteNonQuery() > 0;
        }

        /// <summary>
        ///   Insert faculty.
        ///   If faculty exists, update faculty info.
        /// </summary>
        /// <param name="maKhoa"></param>
        /// <param name="tenKhoa"></param>
        /// <returns> true if insert success </returns>
        public bool insertFaculty(string maKhoa, string tenKhoa) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSKhoa (MaKhoa, TenKhoa)
                    VALUES (@maKhoa, @tenKhoa)
                ", maKhoa, tenKhoa);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Insert class.
        /// </summary>
        /// <param name="maLop"></param>
        /// <param name="tenLop"></param>
        /// <param name="maKhoa"></param>
        /// <returns> true if insert success </returns>
        public bool insertClass(string maLop, string tenLop, string maKhoa) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSLop (MaLop, TenLop, MaKhoa)
                    VALUES (@maLop, @tenLop, @maKhoa)
                ", maLop, tenLop, maKhoa);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Insert subject.
        ///   If subject exists, update subject info.
        /// </summary>
        /// <param name="maMonHoc"></param>
        /// <param name="tenMonHoc"></param>
        /// <returns> true if insert success </returns>
        public bool insertSubject(string maMonHoc, string tenMonHoc) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSMonHoc (MaMonHoc, TenMonHoc)
                    VALUES (@maMonHoc, @tenMonHoc)
                ", maMonHoc, tenMonHoc);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Insert class.
        ///   If class exists, update class info.
        /// </summary>
        /// <param name="maLopHocPhan"></param>
        /// <param name="tenLopHocPhan"></param>
        /// <param name="maKhoa"></param>
        /// <param name="maMonHoc"></param>
        /// <returns> true if insert success </returns>
        public bool insertSubjectClass(string maLopHocPhan, string tenLopHocPhan, string maKhoa, string maMonHoc) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSLopHocPhan (MaLopHocPhan, TenLopHocPhan, MaKhoa, MaMonHoc)
                    VALUES (@maLopHocPhan, @tenLopHocPhan, @maKhoa, @maMonHoc)
                ", maLopHocPhan, tenLopHocPhan, maKhoa, maMonHoc);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Insert class student.
        ///   If class student exists, update class student info.
        /// </summary>
        /// <param name="maSinhVien"></param>
        /// <param name="maLopHocPhan"></param>
        /// <returns> true if insert success </returns>
        public bool insertSubjectClassStudent(string maSinhVien, string maLopHocPhan) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSSinhVienLopHocPhan (MaSinhVien, MaLopHocPhan)
                    VALUES (@MaSinhVien, @maLopHocPhan)
                ", maSinhVien, maLopHocPhan);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Insert score
        ///   If score exists, update score info.
        /// </summary>
        /// <param name="maSinhVien"></param>
        /// <param name="maMonHoc"></param>
        /// <param name="diem"></param>
        /// <returns> true if insert success </returns>
        public bool insertScore(string maSinhVien, string maMonHoc, int diem) {
            try {
                _connector.createAndExecuteCommand(@"
                    INSERT OR REPLACE INTO DSDiem (MaSinhVien, MaMonHoc, Diem)
                    VALUES (@MaSinhVien, @maMonHoc, @diem)
                ", maSinhVien, maMonHoc, diem.ToString());
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }


        public Dictionary<string, string> getStudent(string maSinhVien) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSSinhVien WHERE MaSinhVien = @maSinhVien
            ", maSinhVien);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var student = new Dictionary<string, string>();
            student["MaSinhVien"] = reader.GetString(0);
            student["HoTen"] = reader.GetString(1);
            student["NgaySinh"] = reader.GetString(2);
            student["GioiTinh"] = reader.GetString(3);
            student["MaKhoa"] = reader.GetString(4);
            student["MaLop"] = reader.GetString(5);
            student["LopTruong"] = reader.GetString(6);
            return student;
        }

        public Dictionary<string, string> getFaculty(string maKhoa) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSKhoa WHERE MaKhoa = @maKhoa
            ", maKhoa);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var faculty = new Dictionary<string, string>();
            faculty["MaKhoa"] = reader.GetString(0);
            faculty["TenKhoa"] = reader.GetString(1);
            return faculty;
        }

        public Dictionary<string, string> getClass(string maLop) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSLop WHERE MaLop = @maLop
            ", maLop);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var @class = new Dictionary<string, string>();
            @class["MaLop"] = reader.GetString(0);
            @class["TenLop"] = reader.GetString(1);
            @class["MaKhoa"] = reader.GetString(2);
            return @class;
        }

        public Dictionary<string, string> getSubject(string maMonHoc) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSMonHoc WHERE MaMonHoc = @maMonHoc
            ", maMonHoc);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var subject = new Dictionary<string, string>();
            subject["MaMonHoc"] = reader.GetString(0);
            subject["TenMonHoc"] = reader.GetString(1);
            return subject;
        }

        public Dictionary<string, string> getSubjectClass(string maLopHocPhan) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSLopHocPhan WHERE MaLopHocPhan = @maLopHocPhan
            ", maLopHocPhan);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var subjectClass = new Dictionary<string, string>();
            subjectClass["MaLopHocPhan"] = reader.GetString(0);
            subjectClass["TenLopHocPhan"] = reader.GetString(1);
            subjectClass["MaKhoa"] = reader.GetString(2);
            subjectClass["MaMonHoc"] = reader.GetString(3);
            return subjectClass;
        }

        public Dictionary<string, string> getScore(string maSinhVien, string maMonHoc) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSDiem WHERE MaSinhVien = @maSinhVien AND MaMonHoc = @maMonHoc
            ", maSinhVien, maMonHoc);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var score = new Dictionary<string, string>();
            score["MaSinhVien"] = reader.GetString(0);
            score["MaMonHoc"] = reader.GetString(1);
            score["Diem"] = reader.GetString(2);
            return score;
        }

        public Dictionary<string, string> getStudentClass(string maSinhVien, string maLopHocPhan) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSSinhVienLopHocPhan WHERE MaSinhVien = @maSinhVien AND MaLopHocPhan = @maLopHocPhan
            ", maSinhVien, maLopHocPhan);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var studentClass = new Dictionary<string, string>();
            studentClass["MaSinhVien"] = reader.GetString(0);
            studentClass["MaLopHocPhan"] = reader.GetString(1);
            return studentClass;
        }

        public Dictionary<string, string> getUser(string username) {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSUser WHERE Username = @username
            ", username);
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }

            var user = new Dictionary<string, string>();
            user["Username"] = reader.GetString(0);
            user["Password"] = reader.GetString(1);
            user["MaSinhVien"] = reader.GetString(2);
            return user;
        }

        public List<Dictionary<string, string>> getAllStudent() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSSinhVien
            ");
            var reader = command.ExecuteReader();
            var students = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var student = new Dictionary<string, string>();
                student["MaSinhVien"] = reader.GetString(0);
                student["HoTen"] = reader.GetString(1);
                student["NgaySinh"] = reader.GetString(2);
                student["GioiTinh"] = reader.GetString(3);
                student["MaKhoa"] = reader.GetString(4);
                student["MaLop"] = reader.GetString(5);
                student["LopTruong"] = reader.GetString(6);
                students.Add(student);
            }

            return students;
        }

        public List<Dictionary<string, string>> getAllFaculty() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSKhoa
            ");
            var reader = command.ExecuteReader();
            var faculties = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var faculty = new Dictionary<string, string>();
                faculty["MaKhoa"] = reader.GetString(0);
                faculty["TenKhoa"] = reader.GetString(1);
                faculties.Add(faculty);
            }

            return faculties;
        }

        public List<Dictionary<string, string>> getAllClass() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSLop
            ");
            var reader = command.ExecuteReader();
            var classes = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var @class = new Dictionary<string, string>();
                @class["MaLop"] = reader.GetString(0);
                @class["TenLop"] = reader.GetString(1);
                @class["MaKhoa"] = reader.GetString(2);
                classes.Add(@class);
            }

            return classes;
        }

        public List<Dictionary<string, string>> getAllSubject() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSMonHoc
            ");
            var reader = command.ExecuteReader();
            var subjects = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var subject = new Dictionary<string, string>();
                subject["MaMonHoc"] = reader.GetString(0);
                subject["TenMonHoc"] = reader.GetString(1);
                subjects.Add(subject);
            }

            return subjects;
        }

        public List<Dictionary<string, string>> getAllSubjectClass() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSLopHocPhan
            ");
            var reader = command.ExecuteReader();
            var subjectClasses = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var subjectClass = new Dictionary<string, string>();
                subjectClass["MaLopHocPhan"] = reader.GetString(0);
                subjectClass["TenLopHocPhan"] = reader.GetString(1);
                subjectClass["MaKhoa"] = reader.GetString(2);
                subjectClass["MaMonHoc"] = reader.GetString(3);
                subjectClasses.Add(subjectClass);
            }

            return subjectClasses;
        }

        public List<Dictionary<string, string>> getAllScore() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSDiem
            ");
            var reader = command.ExecuteReader();
            var scores = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var score = new Dictionary<string, string>();
                score["MaSinhVien"] = reader.GetString(0);
                score["MaMonHoc"] = reader.GetString(1);
                score["Diem"] = reader.GetString(2);
                scores.Add(score);
            }

            return scores;
        }

        public List<Dictionary<string, string>> getAllStudentClass() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSSinhVienLopHocPhan
            ");
            var reader = command.ExecuteReader();
            var studentClasses = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var studentClass = new Dictionary<string, string>();
                studentClass["MaSinhVien"] = reader.GetString(0);
                studentClass["MaLopHocPhan"] = reader.GetString(1);
                studentClasses.Add(studentClass);
            }

            return studentClasses;
        }

        public List<Dictionary<string, string>> getAllUser() {
            var command = _connector.createAndExecuteCommand(@"
                SELECT * FROM DSUser
            ");
            var reader = command.ExecuteReader();
            var users = new List<Dictionary<string, string>>();
            while (reader.Read()) {
                var user = new Dictionary<string, string>();
                user["Username"] = reader.GetString(0);
                user["Password"] = reader.GetString(1);
                user["MaSinhVien"] = reader.GetString(2);
                users.Add(user);
            }

            return users;
        }
    }
}