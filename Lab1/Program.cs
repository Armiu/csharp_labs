using System;
using System.Collections.Generic;
using System.Numerics;

namespace Lab1 {
    // Структура DataItem
    public struct DataItem {
        public double X { get; set; }
        public Complex Y1 { get; set; }
        public Complex Y2 { get; set; }

        public DataItem(double x, Complex y1, Complex y2) {
            X = x;
            Y1 = y1;
            Y2 = y2;
        }
        
        public override string ToString() { 
            return $"X: {X}, Y1: {Y1}, Y2: {Y2}";
        }

        public string ToString(string format){
            return $"X: {X.ToString(format)}, Y1: {Y1.ToString(format)}, Y2: {Y2.ToString(format)}";
        }
    }

    // Абстрактный базовый класс V1Data
    public abstract class V1Data {
        public string Key { get; set; }
        public DateTime Date { get; set; }

        public V1Data(string key, DateTime date) {
            Key = key;
            Date = date;
        }

        public abstract int XLength { get; }
        public abstract (double, double) MinMaxDifference { get; }

        public abstract string ToLongString(string format);

        public override string ToString() {
            return $"Key: {Key}, Date: {Date}";
        }        
    }


    // Класс V1DataArray (данные хранятся в массивах)
    public class V1DataArray : V1Data {
        //координаты 𝒙  узлов сетки
        public double[] XArray { get; set; }
        //измеренные значения  пол
        public Complex[] YArray { get; set; }

        public V1DataArray(string key, DateTime date) : base(key, date) {
            XArray = new double[0];
            YArray = new Complex[0];
        }

        public V1DataArray(string key, DateTime date, double[] x, FValues F) : base(key, date) {
            XArray = x;
            YArray = new Complex[x.Length * 2];
            for (int i = 0; i < x.Length; i++) {
                Complex y1 = new Complex();
                Complex y2 = new Complex();
                F(x[i], ref y1, ref y2);
                YArray[i * 2] = y1;
                YArray[i * 2 + 1] = y2;
            }
        }

        public DataItem? this[int index] {
            get {
                if (index >= 0 && index < XArray.Length) {
                    return new DataItem(XArray[index], YArray[2 * index], YArray[2 * index + 1]);
                }
                return null;
            }
        }

        public override int XLength => XArray.Length;

        public override (double, double) MinMaxDifference {
            get {
                if (XArray.Length == 0) return (0, 0);

                double min = double.MaxValue;
                double max = double.MinValue;

                for (int i = 0; i < XArray.Length; i++) {
                    double diff = (YArray[i * 2] - YArray[i * 2 + 1]).Magnitude;
                    if (diff < min) min = diff;
                    if (diff > max) max = diff;
                }

                return (min, max);
            }
        }

        public override string ToString() {
            //Метод ToString базового класса V1Data должен возвращать строку с данными базового класса, 
            //такими как Key и Date
            return $"V1DataArray: {base.ToString()}, Length: {XArray.Length}";
        }

        public override string ToLongString(string format) {
            string result = ToString() + "\n";
            for (int i = 0; i < XArray.Length; i++) {
                result += $"X: {XArray[i].ToString(format)}, Y1: {YArray[2 * i].ToString(format)}, Y2: {YArray[2 * i + 1].ToString(format)}\n";
            }
            return result;
        }
    }

    // Класс V1DataList (данные хранятся в List<DataItem>)
    public class V1DataList : V1Data {
        public List<DataItem> DataList { get; set; } = new List<DataItem>();

        public V1DataList(string key, DateTime date) : base(key, date) { }

        public V1DataList(string key, DateTime date, double[] x, FDI fdi) : base(key, date) {
            //hashset учитывает уникальность объектов, которые в него добавляются
            HashSet<double> uniqueX = new HashSet<double>();
            foreach (double xCoord in x) {
                //Метод Add возвращает true, если элемент успешно добавлен в HashSet, и false, 
                //если элемент уже существует в множестве. Таким образом, если координата xCoord 
                //уникальна, выполняется тело if.
                if (uniqueX.Add(xCoord)) {
                    DataItem data = fdi(xCoord);
                    DataList.Add(data);
                }
            }
        }
        // число узлов сетки
        public override int XLength => DataList.Count;


        //возвращает минимальное и максимальные значения модуля разности
        //компонент поля
        public override (double, double) MinMaxDifference {
            get {
                if (DataList.Count == 0) return (0, 0);
                double min = double.MaxValue, max = double.MinValue;

                foreach (var item in DataList) {
                    // Magnitude возвращает модуль (или абсолютное значение) комплексного числа
                    double difference = (item.Y1 - item.Y2).Magnitude;
                    if (difference < min) min = difference;
                    if (difference > max) max = difference;
                }
                return (min, max);
            }
        }

       //преобразует данные
        // public static explicit operator V1DataArray(V1DataList source) {
        //     double[] xArray = source.DataList.Select(item => item.X).ToArray();
            
        //     // Создаем делегат, который заполняет значения комплексных чисел
        //     FValues fValues = (double x, ref Complex y1, ref Complex y2) => {
        //         var dataItem = source.DataList.First(item => item.X == x);
        //         y1 = dataItem.Y1;
        //         y2 = dataItem.Y2;
        //     };

        //     // Используем делегат в конструкторе V1DataArray
        //     return new V1DataArray(source.Key, source.Date, xArray, fValues);
        // }

        public static explicit operator V1DataArray(V1DataList source) {
            // Создаем новый объект V1DataArray с ключом и датой из source
            V1DataArray result = new V1DataArray(source.Key, source.Date);

            // Устанавливаем размер массивов
            result.XArray = new double[source.DataList.Count];
            result.YArray = new Complex[source.DataList.Count * 2];

            // Заполняем массивы данными из списка
            for (int i = 0; i < source.DataList.Count; i++) {
                result.XArray[i] = source.DataList[i].X;
                result.YArray[i * 2] = source.DataList[i].Y1;
                result.YArray[i * 2 + 1] = source.DataList[i].Y2;
            }

            return result;
        }

        public override string ToString() {
            return $"Type: V1DataList, {base.ToString()}, Count: {DataList.Count}";
        }

        public override string ToLongString(string format) {
            string result = ToString() + "\n";
            foreach (var item in DataList) {
                //добавляется строка с координатой узла сетки X и значениями поля Y1 и Y2
                result += $"X: {item.X.ToString(format)}, Y1: {item.Y1.ToString(format)}, Y2: {item.Y2.ToString(format)}\n";
            }
            return result;
        }
    }

    public delegate void Fy3Values(double x, ref System.Numerics.Complex y3);




       // Класс V1DDataList (данные хранятся в List<Complex>)
    public class V1DDataList : V1DataList {
        public List<Complex> Y3List { get; set; } = new List<Complex>();

        public V1DDataList(string key, DateTime date, double[] x, FDI F, Fy3Values Fy3) : base(key, date, x, F) {
            foreach (var item in DataList) {
                Complex y3 = new Complex();
                Fy3(item.X, ref y3);
                Y3List.Add(y3);
            }
        }

        public override (double, double) MinMaxDifference {
            get {
                if (DataList.Count == 0) return (0, 0);
                double min = double.MaxValue, max = double.MinValue;

                for (int i = 0; i < DataList.Count; i++) {
                    double difference = (DataList[i].Y1 - Y3List[i]).Magnitude;
                    if (difference < min) min = difference;
                    if (difference > max) max = difference;
                }
                return (min, max);
            }
        }

        public override string ToString() {
            return $"Type: V1DDataList, {base.ToString()}";
        }

        public override string ToLongString(string format) {
            string result = ToString() + "\n";
            for (int i = 0; i < DataList.Count; i++) {
                result += $"X: {DataList[i].X.ToString(format)}, Y1: {DataList[i].Y1.ToString(format)}, Y2: {DataList[i].Y2.ToString(format)}, Y3: {Y3List[i].ToString(format)}\n";
            }
            return result;
        }
    }
    
    // Делегаты
    
    public delegate void FValues(double x, ref Complex y1, ref Complex y2);
    public delegate DataItem FDI(double x);

    // Класс V1MainCollection
    public class V1MainCollection : List<V1Data> {

        public V1Data? this[string key] {
            get {
                foreach (var item in this) {
                    if (item.Key == key) return item;
                }
                return null;
            }
        }

        public new bool Add(V1Data v1Data) {
            //добавляет только если в коллекции нет элемента с такими параметрами
            foreach (var item in this) {
                if (item.Key == v1Data.Key && item.Date == v1Data.Date) {
                    return false;
                }
            }
            base.Add(v1Data);
            return true;
        }


        // Конструктор без параметров
        public V1MainCollection() {
            // Добавляем элемент типа V1DataArray
            this.Add(new V1DataArray("Array_1", DateTime.Now, new double[] { 1.0, 2.0, 3.0 }, (double x, ref Complex y1, ref Complex y2) => {
                y1 = new Complex(x, x);
                y2 = new Complex(x + 1, x + 1);
            }));

            // Добавляем элемент типа V1DataList
            this.Add(new V1DataList("List_1", DateTime.Now, new double[] { 1.0, 2.0, 3.0 }, x => new DataItem(x, new Complex(x, x), new Complex(x + 1, x + 1))));

            // Добавляем элемент типа V1DDataList
            this.Add(new V1DDataList("DList_1", DateTime.Now, new double[] { 1.0, 2.0, 3.0 }, x => new DataItem(x, new Complex(x, x), new Complex(x + 1, x + 1)), (double x, ref Complex y3) => {
                y3 = new Complex(x + 2, x + 2);
            }));
        }

        public V1MainCollection(int nA, int nL) {
            for (int i = 0; i < nA; i++) {
                this.Add(new V1DataArray($"Array_{i}", DateTime.Now, new double[] { i, i + 1, i + 2 }, (double x, ref Complex y1, ref Complex y2) => {
                    y1 = new Complex(x, x);
                    y2 = new Complex(x + 1, x + 1);
                }));
            }

            for (int i = 0; i < nL; i++) {
                this.Add(new V1DataList($"List_{i}", DateTime.Now));
            }
        }

        //возвращает строку с информацией о каждом элементе коллекции;
        public string ToLongString(string format) {
            string result = "";
            foreach (var item in this) {
                result += item.ToLongString(format) + "\n";
            }
            return result;
        }

        public override string ToString() {
            string result = "";
            foreach (var item in this) {
                result += item.ToString() + "\n";
            }
            return result;
        }
    }

    class Program {
        static void Main(string[] args) {
            // 1. Создать объект типа V1DataList и вывести его данные
            V1DataList dataList = new V1DataList("List_1", DateTime.Now, new double[] { 1.0, 2.0, 3.0 }, x => new DataItem(x, new Complex(x, x), new Complex(x + 1, x + 1)));
            Console.WriteLine("V1DataList:");
            //"F2" означает, что числа будут отображаться с двумя знаками после запятой
            Console.WriteLine(dataList.ToLongString("F2"));
            // Преобразовать V1DataList в V1DataArray и вывести его данные
            V1DataArray dataArray = (V1DataArray)dataList;
            Console.WriteLine("V1DataArray (after conversion):");
            Console.WriteLine(dataArray.ToLongString("F2"));

            // 2. Создать объект типа V1DataArray и вывести значения индексатора
            V1DataArray dataArray2 = new V1DataArray("Array_1", DateTime.Now, new double[] { 1.0, 2.0, 3.0 }, (double x, ref Complex y1, ref Complex y2) => {
                y1 = new Complex(x, x);
                y2 = new Complex(x + 1, x + 1);
            });
            Console.WriteLine("V1DataArray2:");
            Console.WriteLine(dataArray2.ToLongString("F2"));
            // Значения индексатора
            Console.WriteLine("Indexer values:");
            Console.WriteLine(dataArray2[1]?.ToString() ?? "Index out of range");
            Console.WriteLine(dataArray2[10]?.ToString() ?? "Index out of range");

            // 3. Создать объект типа V1MainCollection и вывести его данные
            V1MainCollection mainCollection = new V1MainCollection(2, 2);
            Console.WriteLine("V1MainCollection:");
            Console.WriteLine(mainCollection.ToLongString("F2"));

            // 4. Вывести значения свойств XLength и MinMaxDifference для каждого элемента
            foreach (var item in mainCollection) {
                Console.WriteLine($"XLength: {item.XLength}, MinMaxDifference: {item.MinMaxDifference}");
            }

            // 5. Вывести значения индексатора с индексом типа string
            Console.WriteLine("Indexer values by key:");
            Console.WriteLine(mainCollection["Array_1"]?.ToString() ?? "Key not found");
            Console.WriteLine(mainCollection["NonExistentKey"]?.ToString() ?? "Key not found");

            //6. доп задание
            V1MainCollection mainCollection1 = new V1MainCollection();
            Console.WriteLine("ADD V1MainCollection:");
            Console.WriteLine(mainCollection1.ToLongString("F2"));

            // Выводим значения свойств XLength и MinMaxDifference для каждого элемента
            foreach (var item in mainCollection1) {
                Console.WriteLine($"XLength: {item.XLength}, MinMaxDifference: {item.MinMaxDifference}");
            }
        }
    }
}

