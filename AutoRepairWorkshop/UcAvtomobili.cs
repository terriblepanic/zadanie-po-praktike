using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AutoRepairWorkshop
{
    public partial class UcAvtomobili : UserControl
    {
        public UcAvtomobili()
        {
            InitializeComponent();
            LoadAvto();
        }

        private void LoadAvto()
        {
            flowLayoutPanel1.Controls.Clear();

            Button addButton = new Button
            {
                Text = "Добавить автомобиль",
                Width = 200,
                Height = 35,
                Margin = new Padding(10)
            };
            addButton.Click += (s, e) => ShowAddAvtoForm();
            flowLayoutPanel1.Controls.Add(addButton);

            using (var conn = Database.GetConnection())
            {
                string query = @"
                SELECT a.kod_avto, a.nomer_kuzova, a.nomer_dvigatelya, a.vladelets, a.zavodskoy_nomer,
                       EXISTS (
                           SELECT 1 FROM remont_avto r
                           WHERE r.kod_avto = a.kod_avto AND r.data_okonchaniya IS NULL
                       ) AS is_in_repair
                FROM avtomobili a";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bool isInRepair = reader.GetBoolean("is_in_repair");
                        string label = isInRepair ? "В ремонте" : "Свободен";
                        int kod = Convert.ToInt32(reader["kod_avto"]);

                        var card = CreateCard(
                            "Автомобиль",
                            "Владелец: " + reader["vladelets"],
                            "Кузов: " + reader["nomer_kuzova"],
                            "Двигатель: " + reader["nomer_dvigatelya"],
                            "Заводской \u2116: " + reader["zavodskoy_nomer"],
                            "Код: " + kod,
                            label,
                            kod
                        );
                        flowLayoutPanel1.Controls.Add(card);
                    }
                }
            }
        }

        private Panel CreateCard(string title, string name, string line1, string line2, string line3, string line4, string status, int kodAvto)
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

            deleteButton.Click += (s, e) =>
            {
                if (MessageBox.Show("Удалить автомобиль?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DeleteAvto(kodAvto);
                    LoadAvto();
                }
            };

            panel.Controls.Add(lblText);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(deleteButton);

            return panel;
        }

        private void DeleteAvto(int kodAvto)
        {
            using (var conn = Database.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    List<int> kodNeispravnosti = new List<int>();
                    string getRemontSql = "SELECT kod_neispravnosti FROM remont_avto WHERE kod_avto = @id";
                    using (var cmdGet = new MySqlCommand(getRemontSql, conn, transaction))
                    {
                        cmdGet.Parameters.AddWithValue("@id", kodAvto);
                        using (var reader = cmdGet.ExecuteReader())
                        {
                            while (reader.Read())
                                kodNeispravnosti.Add(reader.GetInt32(0));
                        }
                    }

                    foreach (int kodNeispr in kodNeispravnosti)
                    {
                        string deleteZap = "DELETE FROM zapchasti WHERE kod_avto = @id AND kod_neispravnosti = @neispr";
                        using (var cmdZap = new MySqlCommand(deleteZap, conn, transaction))
                        {
                            cmdZap.Parameters.AddWithValue("@id", kodAvto);
                            cmdZap.Parameters.AddWithValue("@neispr", kodNeispr);
                            cmdZap.ExecuteNonQuery();
                        }
                    }

                    string deleteRemont = "DELETE FROM remont_avto WHERE kod_avto = @id";
                    using (var cmdRem = new MySqlCommand(deleteRemont, conn, transaction))
                    {
                        cmdRem.Parameters.AddWithValue("@id", kodAvto);
                        cmdRem.ExecuteNonQuery();
                    }

                    string deleteAuto = "DELETE FROM avtomobili WHERE kod_avto = @id";
                    using (var cmdAuto = new MySqlCommand(deleteAuto, conn, transaction))
                    {
                        cmdAuto.Parameters.AddWithValue("@id", kodAvto);
                        cmdAuto.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    MessageBox.Show("Автомобиль успешно удалён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowAddAvtoForm()
        {
            Form addForm = new Form
            {
                Text = "Добавление автомобиля",
                Size = new Size(400, 320),
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox vladeletsBox = new TextBox { Location = new Point(10, 20), Width = 350 };
            Label lblVladelets = new Label { Text = "Владелец", Location = new Point(10, 0), AutoSize = true };

            TextBox kuzovBox = new TextBox { Location = new Point(10, 70), Width = 350 };
            Label lblKuzov = new Label { Text = "Номер кузова", Location = new Point(10, 50), AutoSize = true };

            TextBox dvigBox = new TextBox { Location = new Point(10, 120), Width = 350 };
            Label lblDvig = new Label { Text = "Номер двигателя", Location = new Point(10, 100), AutoSize = true };

            TextBox zavodBox = new TextBox { Location = new Point(10, 170), Width = 350 };
            Label lblZavod = new Label { Text = "Заводской номер", Location = new Point(10, 150), AutoSize = true };

            Button saveButton = new Button { Text = "Сохранить", Location = new Point(10, 220), Width = 100 };

            saveButton.Click += (s, e) =>
            {
                string vladelets = vladeletsBox.Text.Trim();
                string kuzov = kuzovBox.Text.Trim();
                string dvig = dvigBox.Text.Trim();
                string zavod = zavodBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(vladelets) ||
                    string.IsNullOrWhiteSpace(kuzov) ||
                    string.IsNullOrWhiteSpace(dvig) ||
                    string.IsNullOrWhiteSpace(zavod))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (var conn = Database.GetConnection())
                    {
                        int kod = 1;
                        using (var maxCmd = new MySqlCommand("SELECT MAX(kod_avto) FROM avtomobili", conn))
                        {
                            object result = maxCmd.ExecuteScalar();
                            if (result != DBNull.Value)
                                kod = Convert.ToInt32(result) + 1;
                        }

                        string insertSql = @"
                            INSERT INTO avtomobili 
                            (kod_avto, nomer_kuzova, nomer_dvigatelya, vladelets, zavodskoy_nomer) 
                            VALUES 
                            (@kod, @kuzov, @dvig, @vlad, @zavod)";
                        using (var insertCmd = new MySqlCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@kod", kod);
                            insertCmd.Parameters.AddWithValue("@kuzov", kuzov);
                            insertCmd.Parameters.AddWithValue("@dvig", dvig);
                            insertCmd.Parameters.AddWithValue("@vlad", vladelets);
                            insertCmd.Parameters.AddWithValue("@zavod", zavod);

                            insertCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"Автомобиль успешно добавлен.\nПрисвоен код: {kod}", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        addForm.Close();
                        LoadAvto();
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Ошибка при добавлении в базу данных:\n" + ex.Message, "SQL ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Непредвиденная ошибка:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            addForm.Controls.Add(lblVladelets);
            addForm.Controls.Add(vladeletsBox);
            addForm.Controls.Add(lblKuzov);
            addForm.Controls.Add(kuzovBox);
            addForm.Controls.Add(lblDvig);
            addForm.Controls.Add(dvigBox);
            addForm.Controls.Add(lblZavod);
            addForm.Controls.Add(zavodBox);
            addForm.Controls.Add(saveButton);

            addForm.ShowDialog();
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            // необязательная логика перерисовки
        }
    }
}