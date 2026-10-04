using Calc;

int fail = 0;
void T(string keys, string want)
{
    var e = new Engine();
    foreach (var k in keys)
        switch (k)
        {
            case 'C': e.Clear(); break;
            case '<': e.Back(); break;
            case '%': e.Percent(); break;
            case 'n': e.Negate(); break;
            case '=': e.Equals(); break;
            case '+' or '-' or '*' or '/': e.Op(k); break;
            default: e.Digit(k); break;
        }
    if (e.Display != want) { fail++; Console.WriteLine($"FAIL {keys}: got {e.Display}, want {want}"); }
}
T("12+7=", "19");
T("2+3*4=", "20");
T("10/4=", "2.5");
T("5/0=", "Error");
T("5/0=3+1=", "4");
T("1.2.3", "1.23");
T("123<", "12");
T("5n", "-5");
T("200+10%", "20");
T("50%", "0.5");
T("9-9=", "0");
T("1/3*3=", "1");
T("2+=", "4");
T("7==", "7");
Console.WriteLine(fail == 0 ? "ALL PASS" : $"{fail} failed");
return fail;
