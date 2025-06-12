using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace AutoRepairWorkshop
{
    public partial class UcRemont : UserControl
    {
        public UcRemont()
        {
            InitializeComponent();
            LoadRepairs();
        }

        // Загружает список ремонтов и формирует карточки
        private void LoadRepairs()
        {
            flowLayoutPanel1.Controls.Clear();

            Button addButton = new Button
            {
                Text = "Добавить ремонт",
                Width = 200,
                Height = 35,
                Margin = new Padding(10)
            };
            addButton.Click += (s, e) => ShowAddRepairForm();
            flowLayoutPanel1.Controls.Add(addButton);

            using (var conn = Database.GetConnection())
            {
                string query = @"
                    SELECT ra.kod_avto, ra.kod_neispravnosti, ra.data_postupleniya, ra.data_okonchaniya,
                           ra.kod_brigady, ra.opisanie,
                           b.naimenovanie AS brigada,
                           n.naimenovanie AS neispravnost
                    FROM remont_avto ra
                    LEFT JOIN brigady b ON ra.kod_brigady = b.kod_brigady
                    LEFT JOIN neispravnosti n ON ra.kod_neispravnosti = n.kod_neispravnosti";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bool isDone = reader["data_okonchaniya"] != DBNull.Value;
                        string status = isDone ? "✔ Завершено" : "⏳ В процессе";

                        DateTime post = Convert.ToDateTime(reader["data_postupleniya"]);
                        string kodAvto = reader["kod_avto"].ToString();
                        string kodNeispr = reader["kod_neispravnosti"].ToString();
                        int kodBrig = Convert.ToInt32(reader["kod_brigady"]);
                        string opisanie = reader["opisanie"].ToString();
                        DateTime? endDate = reader["data_okonchaniya"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["data_okonchaniya"]) : null;

                        var card = CreateCard(
                            "Ремонт",
                            "Авто: " + kodAvto,
                            "Неисправность: " + reader["neispravnost"],
                            "Поступление: " + post.ToShortDateString(),
                            "Завершение: " + (isDone ? Convert.ToDateTime(reader["data_okonchaniya"]).ToShortDateString() : "—"),
                            "Бригада: " + reader["brigada"],
                            status,
                            kodAvto,
                            kodNeispr,
                            post,
                            kodBrig,
                            opisanie,
                            endDate
                        );
                        flowLayoutPanel1.Controls.Add(card);
                    }
                }
            }
        }

        // Создаёт карточку ремонта
        // Передаём дополнительные данные для возможности редактирования
        private Panel CreateCard(
            string title,
            string name,
            string line1,
            string line2,
            string line3,
            string line4,
            string status,
            string kodAvto,
            string kodNeispr,
            DateTime postDate,
            int kodBrig,
            string opisanie,
            DateTime? endDate)
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
                if (MessageBox.Show("Удалить ремонт?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DeleteRepair(Convert.ToInt32(kodAvto), Convert.ToInt32(kodNeispr), postDate);
                    LoadRepairs();
                }
            };

            editButton.Click += (s, e) =>
            {
                ShowEditRepairForm(Convert.ToInt32(kodAvto), Convert.ToInt32(kodNeispr), postDate, kodBrig, opisanie, endDate);
            };

            panel.Controls.Add(lblText);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(deleteButton);
            panel.Controls.Add(editButton);

            return panel;
        }

        private void DeleteRepair(int kodAvto, int kodNeispr, DateTime postuplenie)
        {
            using (var conn = Database.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. Удалить все запчасти, связанные с этим ремонтом
                    string deleteParts = "DELETE FROM zapchasti WHERE kod_avto = @a AND kod_neispravnosti = @n";
                    using (var cmdZap = new MySqlCommand(deleteParts, conn, transaction))
                    {
                        cmdZap.Parameters.AddWithValue("@a", kodAvto);
                        cmdZap.Parameters.AddWithValue("@n", kodNeispr);
                        cmdZap.ExecuteNonQuery();
                    }

                    // 2. Удалить сам ремонт
                    string deleteRepair = "DELETE FROM remont_avto WHERE kod_avto = @a AND kod_neispravnosti = @n AND data_postupleniya = @d";
                    using (var cmdRem = new MySqlCommand(deleteRepair, conn, transaction))
                    {
                        cmdRem.Parameters.AddWithValue("@a", kodAvto);
                        cmdRem.Parameters.AddWithValue("@n", kodNeispr);
                        cmdRem.Parameters.AddWithValue("@d", postuplenie);
                        cmdRem.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    MessageBox.Show("Ремонт и связанные запчасти удалены.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        // Окно добавления нового ремонта
        private void ShowAddRepairForm()
        {
            Form form = new Form
            {
                Text = "Добавление ремонта",
                Size = new Size(400, 330),
                StartPosition = FormStartPosition.CenterParent
            };

            NumericUpDown kodAvtoBox = new NumericUpDown { Minimum = 1, Maximum = 999999, Location = new Point(10, 20), Width = 350 };
            Label lblAvto = new Label { Text = "Код автомобиля", Location = new Point(10, 0), AutoSize = true };

            NumericUpDown kodNeisprBox = new NumericUpDown { Minimum = 1, Maximum = 999999, Location = new Point(10, 70), Width = 350 };
            Label lblNeispr = new Label { Text = "Код неисправности", Location = new Point(10, 50), AutoSize = true };

            NumericUpDown kodBrigBox = new NumericUpDown { Minimum = 1, Maximum = 999999, Location = new Point(10, 120), Width = 350 };
            Label lblBrig = new Label { Text = "Код бригады", Location = new Point(10, 100), AutoSize = true };

            TextBox opisanieBox = new TextBox { Location = new Point(10, 170), Width = 350 };
            Label lblOpis = new Label { Text = "Описание", Location = new Point(10, 150), AutoSize = true };

            Button saveButton = new Button { Text = "Сохранить", Location = new Point(10, 220), Width = 100 };

            saveButton.Click += (s, e) =>
            {
                int kodAvto = (int)kodAvtoBox.Value;
                int kodNeispr = (int)kodNeisprBox.Value;
                int kodBrig = (int)kodBrigBox.Value;
                string opisanie = opisanieBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(opisanie))
                {
                    MessageBox.Show("Описание не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = Database.GetConnection())
                {
                    // Валидация: авто
                    if (!Exists(conn, "SELECT COUNT(*) FROM avtomobili WHERE kod_avto = @id", "@id", kodAvto))
                    {
                        MessageBox.Show("Автомобиль с таким кодом не существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Валидация: неисправность
                    if (!Exists(conn, "SELECT 1 FROM neispravnosti WHERE kod_neispravnosti = @id", "@id", kodNeispr))
                    {
                        MessageBox.Show("Неисправность с таким кодом не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Валидация: бригада
                    if (!Exists(conn, "SELECT 1 FROM brigady WHERE kod_brigady = @id", "@id", kodBrig))
                    {
                        MessageBox.Show("Бригада с таким кодом не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Вставка
                    string insert = @"
                INSERT INTO remont_avto 
                (kod_avto, kod_neispravnosti, data_postupleniya, kod_brigady, opisanie)
                VALUES 
                (@avto, @neispr, @date, @brig, @opis)";
                    using (var cmd = new MySqlCommand(insert, conn))
                    {
                        cmd.Parameters.AddWithValue("@avto", kodAvto);
                        cmd.Parameters.AddWithValue("@neispr", kodNeispr);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@brig", kodBrig);
                        cmd.Parameters.AddWithValue("@opis", opisanie);

                        try
                        {
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Ремонт добавлен.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            form.Close();
                            LoadRepairs();
                        }
                        catch (MySqlException ex)
                        {
                            MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            form.Controls.Add(lblAvto);
            form.Controls.Add(kodAvtoBox);
            form.Controls.Add(lblNeispr);
            form.Controls.Add(kodNeisprBox);
            form.Controls.Add(lblBrig);
            form.Controls.Add(kodBrigBox);
            form.Controls.Add(lblOpis);
            form.Controls.Add(opisanieBox);
            form.Controls.Add(saveButton);

            form.ShowDialog();
        }

        // Форма редактирования данных ремонта
        private void ShowEditRepairForm(int kodAvto, int kodNeispr, DateTime post, int kodBrig, string opis, DateTime? endDate)
        {
            Form form = new Form
            {
                Text = "Редактирование ремонта",
                Size = new Size(400, 360),
                StartPosition = FormStartPosition.CenterParent
            };

            NumericUpDown brigBox = new NumericUpDown { Minimum = 1, Maximum = 999999, Location = new Point(10, 20), Width = 350, Value = kodBrig };
            Label lblBrig = new Label { Text = "Код бригады", Location = new Point(10, 0), AutoSize = true };

            TextBox opisBox = new TextBox { Location = new Point(10, 70), Width = 350, Text = opis };
            Label lblOpis = new Label { Text = "Описание", Location = new Point(10, 50), AutoSize = true };

            DateTimePicker endPicker = new DateTimePicker { Location = new Point(10, 120), Width = 350, Format = DateTimePickerFormat.Short };
            endPicker.Value = endDate ?? DateTime.Now.Date;
            endPicker.Checked = endDate.HasValue;
            Label lblEnd = new Label { Text = "Дата окончания", Location = new Point(10, 100), AutoSize = true };

            Button saveButton = new Button { Text = "Сохранить", Location = new Point(10, 170), Width = 100 };

            saveButton.Click += (s, e) =>
            {
                int newBrig = (int)brigBox.Value;
                string newOpis = opisBox.Text.Trim();
                DateTime? newEnd = endPicker.Checked ? (DateTime?)endPicker.Value.Date : null;

                if (string.IsNullOrWhiteSpace(newOpis))
                {
                    MessageBox.Show("Описание не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = Database.GetConnection())
                using (var cmd = new MySqlCommand(@"UPDATE remont_avto SET kod_brigady=@b, opisanie=@o, data_okonchaniya=@d WHERE kod_avto=@a AND kod_neispravnosti=@n AND data_postupleniya=@p", conn))
                {
                    cmd.Parameters.AddWithValue("@b", newBrig);
                    cmd.Parameters.AddWithValue("@o", newOpis);
                    cmd.Parameters.AddWithValue("@d", (object?)newEnd ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@a", kodAvto);
                    cmd.Parameters.AddWithValue("@n", kodNeispr);
                    cmd.Parameters.AddWithValue("@p", post);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Запись обновлена", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.Close();
                        LoadRepairs();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка обновления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            form.Controls.Add(lblBrig);
            form.Controls.Add(brigBox);
            form.Controls.Add(lblOpis);
            form.Controls.Add(opisBox);
            form.Controls.Add(lblEnd);
            form.Controls.Add(endPicker);
            form.Controls.Add(saveButton);

            form.ShowDialog();
        }

        private bool Exists(MySqlConnection conn, string query, string param, object value)
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue(param, value);
                object result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
        }




        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            // Необязательная отрисовка
        }
    }
}
