using System;
using System.Globalization;
using System.Windows.Forms;

public class CustomNumericUpDown : NumericUpDown
{
    private bool _isProgrammaticChange = false;

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        // Заменяем запятую на точку при вводе
        if (e.KeyChar == ',')
        {
            e.KeyChar = '.';
        }
        base.OnKeyPress(e);
    }

    protected override void UpdateEditText()
    {
        // Отображаем значение с точкой независимо от региональных настроек
        this.Text = this.Value.ToString("0.########", CultureInfo.InvariantCulture);
    }

    protected override void ValidateEditText()
    {
        // Если изменение программное - пропускаем валидацию
        if (_isProgrammaticChange) return;

        try
        {
            // Если строка пустая, устанавливаем минимальное значение (или 0)
            if (string.IsNullOrWhiteSpace(this.Text))
            {
                this.Value = this.Minimum;
                return;
            }

            // Заменяем запятую на точку и парсим
            string text = this.Text.Replace(',', '.');
            decimal newValue = decimal.Parse(text, CultureInfo.InvariantCulture);

            if (newValue < this.Minimum)
                this.Value = this.Minimum;
            else if (newValue > this.Maximum)
                this.Value = this.Maximum;
            else
                this.Value = newValue;
        }
        catch
        {
            // Если ввод некорректен - откат к текущему значению (без рекурсии)
            _isProgrammaticChange = true;
            this.Text = this.Value.ToString("0.########", CultureInfo.InvariantCulture);
            _isProgrammaticChange = false;
        }
    }

    public new decimal Value
    {
        get => base.Value;
        set
        {
            _isProgrammaticChange = true;
            base.Value = value;
            _isProgrammaticChange = false;
        }
    }
}