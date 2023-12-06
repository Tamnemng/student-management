﻿using System;
using System.Windows.Forms;

namespace studentManagement {
    public partial class subjectForm : Form {
        public subjectForm() {
            InitializeComponent();
            loadAllFaculty();
            createColumsListViewAddScore();
            createColumsListViewDisplaySubject();
        }

        private Database db = new Database(Program.dbLocation);
        private bool isEditing = false; // Add a class-level variable to keep track of the state

        private bool isSubject = true;

        private void AutoResizeListViewColumns(System.Windows.Forms.ListView listView) {
            foreach (System.Windows.Forms.ColumnHeader column in listView.Columns) {
                column.AutoResize(System.Windows.Forms.ColumnHeaderAutoResizeStyle.HeaderSize);
                //column.Width = -1;
                //column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private void addAndEditForm(bool bCheck) {
            isEditing = bCheck;
            if (isEditing) {
                gbAddSubject.Text = "Chỉnh Sửa Môn Học";
                btnAddSubject.Text = "Lưu Chỉnh Sửa";
                labelDeleteSubject.Text = "Chọn Môn Học";
                btnEditSubject.Text = "Thêm Môn Học";
                btnDeleteSubject.Text = "Chọn";
                bCheck = false;
            } else {
                gbAddSubject.Text = "Thêm Môn Học";
                btnAddSubject.Text = "Thêm";
                labelDeleteSubject.Text = "Xóa Môn Học";
                btnEditSubject.Text = "Chỉnh Sửa Môn Học";
                btnDeleteSubject.Text = "Xóa";
                bCheck = true;
            }
        }

        private void addSubjectForm(int iNum) {
            if (iNum == 0) gbAddSubject.Show();
            else gbAddSubject.Hide();
        }

        private void addScoreForm(int iNum) {
            if (iNum == 0) gbAddScore.Show();
            else gbAddScore.Hide();
        }
        private void btnReturnMenu_Click(object sender, EventArgs e) {
            Close();
        }

        private void btnEditSubject_Click(object sender, EventArgs e) {
            addAndEditForm(!isEditing);
            clearSelectedComboBoxAndListView();
        }

        private void btnAddSubjectAddScore_Click(object sender, EventArgs e) {
            addSubjectForm(0);
            addScoreForm(1);
            clearSelectedComboBoxAndListView();
            isSubject = true;
        }

        private void btnAddScore_Click(object sender, EventArgs e) {
            addScoreForm(0);
            addSubjectForm(1);
            clearSelectedComboBoxAndListView();
            isSubject = false;
        }

        //lay DS diem mon hoc
        private void loadAllFaculty() {
            var faculties = db.getAllFaculties();
            foreach (var faculty in faculties) {
                comboBoxFaculty.Items.Add(faculty["TenKhoa"]);
                comboBox1.Items.Add(faculty["TenKhoa"]);
                comboBox2.Items.Add(faculty["TenKhoa"]);
            }
        }

        private void loadSubject() {
            comboBoxSubject.Items.Clear();
            comboBoxSubject.Text = "";
            comboBoxSubject.Refresh();
            comboBoxSubject.SelectedIndex = -1;
            listViewDisplaySubject.Items.Clear();

            var subject = db.getAllSubject();

            foreach (var row in subject) {
                var temp = db.getFaculty(row["MaKhoa"].ToString()); ;
                if (temp != null) {
                    if (comboBoxFaculty.SelectedIndex != -1 && comboBoxFaculty.SelectedItem.ToString() == temp["TenKhoa"].ToString()) {
                        comboBoxSubject.Items.Add(row["TenMonHoc"]);
                    }
                }
                AutoResizeListViewColumns(listViewDisplaySubject);
            }
            comboBoxClass.Items.Clear();
            comboBoxClass.SelectedIndex = -1;
            comboBoxClass.Text = "";
            listViewAddScore.Items.Clear();
        }

        private void loadSubjectClass() {
            comboBoxClass.Items.Clear();
            comboBoxClass.SelectedIndex = -1;
            comboBoxClass.Text = "";
            listViewAddScore.Items.Clear();

            var subjectClass = db.getAllSubjectClass();
            if (comboBoxSubject.SelectedIndex != -1)
                foreach (var row in subjectClass) {
                    var temp = db.getSubject(row["MaMonHoc"].ToString());
                    if (comboBoxSubject.SelectedItem.ToString() == temp["TenMonHoc"].ToString())
                        comboBoxClass.Items.Add(row["TenLopHocPhan"]);
                }
        }

        private void createColumsListViewAddScore() {
            listViewAddScore.View = View.Details;
            listViewAddScore.FullRowSelect = true;
            listViewAddScore.GridLines = true;
            listViewAddScore.Columns.Add("Mã Sinh Viên", 100);
            listViewAddScore.Columns.Add("Họ Tên", 100);
            listViewAddScore.Columns.Add("Mã Lớp", 100);
            listViewAddScore.Columns.Add("Điểm", 100);

            AutoResizeListViewColumns(listViewAddScore);
        }

        private void createColumsListViewDisplaySubject() {
            listViewDisplaySubject.View = View.Details;
            listViewDisplaySubject.FullRowSelect = true;
            listViewDisplaySubject.GridLines = true;
            listViewDisplaySubject.Columns.Add("Mã Môn Học", 100);
            listViewDisplaySubject.Columns.Add("Tên Môn Học", 100);
            listViewDisplaySubject.Columns.Add("Số Tín Chỉ", 100);

            AutoResizeListViewColumns(listViewDisplaySubject);
        }

        private void loadListViewAddScore() {
            listViewAddScore.Items.Clear();

            var maLop = getSubjectClassID(comboBoxClass.SelectedItem.ToString());

            var subClass = db.getSubjectClass(getSubjectClassID(maLop));

            var subjectClassStudentList = db.getAllSubjectClassStudents(); 

            foreach(var row in subjectClassStudentList) {
                if (row["MaLopHocPhan"].ToString() == maLop) {
                    var student = db.getStudent(row["MaSinhVien"].ToString());
                    var item = new ListViewItem(student["MaSinhVien"].ToString());
                    item.SubItems.Add(student["HoTen"].ToString());
                    item.SubItems.Add(row["MaLopHocPhan"].ToString());
                    //item.SubItems.Add(row["Diem"].ToString());
                    listViewAddScore.Items.Add(item);
                }
            }

            AutoResizeListViewColumns(listViewAddScore);
        }

        private string getFacultyID(string facultyName) {
            var result = "";

            var faculties = db.getAllFaculties();
            foreach (var faculty in faculties) {
                if (facultyName == faculty["TenKhoa"].ToString()) {
                    result = faculty["MaKhoa"].ToString();
                    break;
                }
            }

            return result;
        }

        private string getSubjectID(string subjectName) {
            var result = "";

            var subjects = db.getAllSubject();
            foreach (var subject in subjects) {
                if (subjectName == subject["TenMonHoc"].ToString()) {
                    result = subject["MaMonHoc"].ToString();
                    break;
                }
            }

            return result;
        }

        private string getClassID(string className) {
            var result = "";

            var classes = db.getAllClasses();
            foreach (var _class in classes) {
                if (className == _class["TenLop"].ToString()) {
                    result = _class["MaLop"].ToString();
                    break;
                }
            }

            return result;
        }

        private string getSubjectClassID(string subjectClassName) {
            var result = "";

            var subjectClasses = db.getAllSubjectClass();
            foreach (var subjectClass in subjectClasses) {
                if (subjectClassName == subjectClass["TenLopHocPhan"].ToString()) {
                    result = subjectClass["MaLopHocPhan"].ToString();
                    break;
                }
            }

            return result;
        }

        private string getStudentID(string studentName) {
            var result = "";

            var students = db.getAllStudents();
            foreach (var student in students) {
                if (studentName == student["HoTen"].ToString()) {
                    result = student["MaSinhVien"].ToString();
                    break;
                }
            }

            return result;
        }


        private void loadListViewDisplaySubject(string MaKhoa) {
            listViewDisplaySubject.Items.Clear();

            var subject = db.getAllSubject();
            foreach (var row in subject) {
                if (row["MaKhoa"].ToString() == MaKhoa) {
                    var item = new ListViewItem(row["MaMonHoc"].ToString());
                    item.SubItems.Add(row["TenMonHoc"].ToString());
                    item.SubItems.Add(row["SoTinChi"].ToString());
                    listViewDisplaySubject.Items.Add(item);
                }
            }

            AutoResizeListViewColumns(listViewDisplaySubject);
        }

        private void comboBoxSubject_SelectedIndexChanged(object sender, EventArgs e) {
            loadSubjectClass();
        }

        private void comboBoxFaculty_SelectedIndexChanged(object sender, EventArgs e) {
            loadSubject();
        }

        private void comboBoxClass_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboBoxClass.SelectedIndex != -1)
                loadListViewAddScore();
        }

        private void listViewAddScore_SelectedIndexChanged(object sender, EventArgs e) {
            txtStudentName.Text = listViewAddScore.SelectedItems[0].SubItems[1].Text;
            txtMssvAddScore.Text = listViewAddScore.SelectedItems[0].SubItems[0].Text;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            listViewDisplaySubject.Items.Clear();
            var faculties = db.getAllFaculties();
            if (comboBox2.SelectedIndex != -1)
                foreach (var faculty in faculties) {
                    if (comboBox2.SelectedItem.ToString() == faculty["TenKhoa"].ToString()) {
                        loadListViewDisplaySubject(faculty["MaKhoa"].ToString());
                    }
                }
        }

        private void clearSelectedComboBoxAndListView() {
            comboBoxFaculty.SelectedIndex = -1;
            comboBoxSubject.Items.Clear();
            comboBoxSubject.SelectedIndex = -1;
            comboBoxSubject.Text = "";
            comboBoxClass.Items.Clear();
            comboBoxClass.SelectedIndex = -1;
            comboBoxClass.Text = "";
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            listViewAddScore.Items.Clear();
            listViewDisplaySubject.Items.Clear();
            txtMssvAddScore.Clear();
            txtStudentName.Clear();
            txtCredit.Clear();
            textBox1.Clear();
            textBox2.Clear();
        }

        private void btnDeleteSubject_Click(object sender, EventArgs e) {
            if (isEditing && listViewDisplaySubject.SelectedItems.Count == 1) {
                textBox1.Text = "";
                textBox2.Text = "";
                comboBox1.SelectedIndex = -1;
                textBox1.Text = listViewDisplaySubject.SelectedItems[0].SubItems[1].Text;
                textBox1.Focus();
                textBox2.Text = listViewDisplaySubject.SelectedItems[0].SubItems[0].Text;
                txtCredit.Text = listViewDisplaySubject.SelectedItems[0].SubItems[2].Text;
                comboBox1.SelectedIndex = comboBox2.SelectedIndex;
            }
        }
    }
}
