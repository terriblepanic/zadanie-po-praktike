using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace AutoRepairWorkshop
{
    public partial class UcPersonnel : UserControl
    {
        public UcPersonnel()
        {
            InitializeComponent();
            LoadPersonnel();
        }

        // Получает список сотрудников из базы и формирует карточки
        private void LoadPersonnel()
        {
            flowLayoutPanel1.Controls.Clear();

            Button addButton = new Button
            {
                Text = "Добавить сотрудника",
                Width = 200,
                Height = 35,
                Margin = new Padding(10)
            };
            addButton.Click += (s, e) => ShowAddPersonnelForm();
            flowLayoutPanel1.Controls.Add(addButton);

            using (var conn = Database.GetConnection())
            {
                string query = @"
                    SELECT p.fio, p.dolzhnost, p.inn,
                           IFNULL(b.naimenovanie, '—') AS brigada,
                           IFNULL(m.naimenovanie, '—') AS masterskaya,
                           p.kod_brigady,
                           p.kod_masterskoy,
                           (SELECT COUNT(*) FROM remont_avto r WHERE r.kod_brigady = p.kod_brigady) AS repair_count
                    FROM personal p
                    LEFT JOIN brigady b ON p.kod_brigady = b.kod_brigady
                    LEFT JOIN masterskie m ON p.kod_masterskoy = m.kod_masterskoy";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fio = reader["fio"].ToString();
                        string dolzhnost = reader["dolzhnost"].ToString();
                        string inn = reader["inn"].ToString();
                        string brigada = reader["brigada"].ToString();
                        string masterskaya = reader["masterskaya"].ToString();
                        int repairCount = Convert.ToInt32(reader["repair_count"]);
                        int kodBrig = Convert.ToInt32(reader["kod_brigady"]);
                        int kodMaster = Convert.ToInt32(reader["kod_masterskoy"]);
                        string status = repairCount > 0 ? $"{repairCount} ремонт(ов)" : "Нет ремонтов";

                        var card = CreateCard(
                            title: "Сотрудник",
                            name: fio,
                            line1: "Должность: " + dolzhnost,
                            line2: "ИНН: " + inn,
                            line3: "Мастерская: " + masterskaya,
                            line4: "Бригада: " + brigada,
                            status: status,
                            inn: inn,
                            fio: fio,
                            dolzhnost: dolzhnost,
                            kodBrig: kodBrig,
                            kodMaster: kodMaster
                        );
                        flowLayoutPanel1.Controls.Add(card);
                    }
                }
            }
        }

        // Создаёт карточку сотрудника
        // Передаются также исходные значения полей для редактирования
        private Panel CreateCard(
            string title,
            string name,
            string line1,
            string line2,
            string line3,
            string line4,
            string status,
            string inn,
            string fio,
            string dolzhnost,
            int kodBrig,
            int kodMaster)
        {
            Panel panel = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                Width = 750,
                Height = 130,
                Margin = new Padding(10)
            };

            Label lblText = new Label
            {
                Text = $"{title}\n{name}\n{line1}\n{line2}\n{line3}\n{line4}",
                AutoSize = true,
                Location = new Point(0, 10),
                MaximumSize = new Size(600, 0)
            };

            Label lblStatus = new Label
            {
                Text = status,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                TextAlign = ContentAlignment.TopRight,
                Location = new Point(panel.Width - 150, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Button deleteButton = new Button
            {
                Text = "Удалить",
                Location = new Point(10, panel.Height - 35),
                Width = 100
            };

            Button editButton = new Button
            {
                Text = "Редактировать",
                Location = new Point(120, panel.Height - 35),
                Width = 100
            };

            deleteButton.Click += (s, e) =>
            {
                if (MessageBox.Show("Удалить сотрудника?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DeletePersonnel(inn);
                    LoadPersonnel();
                }
            };

            editButton.Click += (s, e) =>
            {
                ShowEditPersonnelForm(fio, dolzhnost, inn, kodBrig, kodMaster);
            };

            panel.Controls.Add(lblText);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(deleteButton);
            panel.Controls.Add(editButton);

            return panel;
        }

        private void DeletePersonnel(string inn)
        {
            using (var conn = Database.GetConnection())
            {
                string query = "DELETE FROM personal WHERE inn = @inn";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@inn", inn);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Сотрудник успешно удалён.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Окно добавления нового сотрудника
        private void ShowAddPersonnelForm()
        {
            Form addForm = new Form
            {
                Text = "Добавление сотрудника",
                Size = new Size(400, 340),
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox fioBox = new TextBox { Location = new Point(10, 20), Width = 350 };
            Label fioLabel = new Label { Text = "ФИО", Location = new Point(10, 0), AutoSize = true };

            TextBox dolzhnostBox = new TextBox { Location = new Point(10, 70), Width = 350 };
            Label dolzhnostLabel = new Label { Text = "Должность", Location = new Point(10, 50), AutoSize = true };

            TextBox innBox = new TextBox { Location = new Point(10, 120), Width = 350 };
            Label innLabel = new Label { Text = "ИНН", Location = new Point(10, 100), AutoSize = true };

            NumericUpDown brigadaBox = new NumericUpDown { Minimum = 1, Maximum = 100, Location = new Point(10, 170), Width = 350 };
            Label brigadaLabel = new Label { Text = "Код бригады", Location = new Point(10, 150), AutoSize = true };

            NumericUpDown masterskayaBox = new NumericUpDown { Minimum = 1, Maximum = 100, Location = new Point(10, 220), Width = 350 };
            Label masterskayaLabel = new Label { Text = "Код мастерской", Location = new Point(10, 200), AutoSize = true };

            Button saveButton = new Button { Text = "Сохранить", Location = new Point(10, 270), Width = 100 };

            saveButton.Click += (s, e) =>
            {
                string fio = fioBox.Text.Trim();
                string dolzhnost = dolzhnostBox.Text.Trim();
                string inn = innBox.Text.Trim();
                int brigada = (int)brigadaBox.Value;
                int masterskaya = (int)masterskayaBox.Value;

                if (string.IsNullOrWhiteSpace(fio) ||
                    string.IsNullOrWhiteSpace(dolzhnost) ||
                    string.IsNullOrWhiteSpace(inn))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = Database.GetConnection())
                {
                    string checkSql = "SELECT COUNT(*) FROM personal WHERE inn = @inn";
                    using (var checkCmd = new MySqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@inn", inn);
                        int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (exists > 0)
                        {
                            MessageBox.Show("Сотрудник с таким ИНН уже существует.", "Дубликат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    string insert = @"
                        INSERT INTO personal (fio, dolzhnost, inn, kod_brigady, kod_masterskoy)
                        VALUES (@fio, @dolzhnost, @inn, @brigada, @masterskaya)";
                    using (var cmd = new MySqlCommand(insert, conn))
                    {
                        cmd.Parameters.AddWithValue("@fio", fio);
                        cmd.Parameters.AddWithValue("@dolzhnost", dolzhnost);
                        cmd.Parameters.AddWithValue("@inn", inn);
                        cmd.Parameters.AddWithValue("@brigada", brigada);
                        cmd.Parameters.AddWithValue("@masterskaya", masterskaya);

                        try
                        {
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Сотрудник успешно добавлен.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            addForm.Close();
                            LoadPersonnel();
                        }
                        catch (MySqlException ex)
                        {
                            MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            addForm.Controls.Add(fioLabel);
            addForm.Controls.Add(fioBox);
            addForm.Controls.Add(dolzhnostLabel);
            addForm.Controls.Add(dolzhnostBox);
            addForm.Controls.Add(innLabel);
            addForm.Controls.Add(innBox);
            addForm.Controls.Add(brigadaLabel);
            addForm.Controls.Add(brigadaBox);
            addForm.Controls.Add(masterskayaLabel);
            addForm.Controls.Add(masterskayaBox);
            addForm.Controls.Add(saveButton);

            addForm.ShowDialog();
        }

        // Форма редактирования сотрудника
        private void ShowEditPersonnelForm(string fio, string dolzhnost, string inn, int kodBrigady, int kodMasterskoy)
        {
            Form editForm = new Form
            {
                Text = "Редактирование сотрудника",
                Size = new Size(400, 340),
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox fioBox = new TextBox { Location = new Point(10, 20), Width = 350, Text = fio };
            Label fioLabel = new Label { Text = "ФИО", Location = new Point(10, 0), AutoSize = true };

            TextBox dolzhnostBox = new TextBox { Location = new Point(10, 70), Width = 350, Text = dolzhnost };
            Label dolzhnostLabel = new Label { Text = "Должность", Location = new Point(10, 50), AutoSize = true };

            NumericUpDown brigBox = new NumericUpDown { Minimum = 1, Maximum = 100, Location = new Point(10, 120), Width = 350, Value = kodBrigady };
            Label brigLabel = new Label { Text = "Код бригады", Location = new Point(10, 100), AutoSize = true };

            NumericUpDown mastBox = new NumericUpDown { Minimum = 1, Maximum = 100, Location = new Point(10, 170), Width = 350, Value = kodMasterskoy };
            Label mastLabel = new Label { Text = "Код мастерской", Location = new Point(10, 150), AutoSize = true };

            Button saveButton = new Button { Text = "Сохранить", Location = new Point(10, 220), Width = 100 };

            saveButton.Click += (s, e) =>
            {
                string newFio = fioBox.Text.Trim();
                string newDolz = dolzhnostBox.Text.Trim();
                int newBrig = (int)brigBox.Value;
                int newMast = (int)mastBox.Value;

                if (string.IsNullOrWhiteSpace(newFio) || string.IsNullOrWhiteSpace(newDolz))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = Database.GetConnection())
                using (var cmd = new MySqlCommand(@"UPDATE personal SET fio=@fio, dolzhnost=@dol, kod_brigady=@b, kod_masterskoy=@m WHERE inn=@inn", conn))
                {
                    cmd.Parameters.AddWithValue("@fio", newFio);
                    cmd.Parameters.AddWithValue("@dol", newDolz);
                    cmd.Parameters.AddWithValue("@b", newBrig);
                    cmd.Parameters.AddWithValue("@m", newMast);
                    cmd.Parameters.AddWithValue("@inn", inn);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные обновлены", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        editForm.Close();
                        LoadPersonnel();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка обновления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            editForm.Controls.Add(fioLabel);
            editForm.Controls.Add(fioBox);
            editForm.Controls.Add(dolzhnostLabel);
            editForm.Controls.Add(dolzhnostBox);
            editForm.Controls.Add(brigLabel);
            editForm.Controls.Add(brigBox);
            editForm.Controls.Add(mastLabel);
            editForm.Controls.Add(mastBox);
            editForm.Controls.Add(saveButton);

            editForm.ShowDialog();
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
