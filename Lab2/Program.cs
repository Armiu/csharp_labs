using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    public abstract class V1Data : IEnumerable<DataItem> {
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

        public abstract IEnumerator<DataItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }


    // Класс V1DataArray (данные хранятся в массивах)
    public class V1DataArray : V1Data, IEnumerable<DataItem> {
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
            return $"V1DataArray: {base.ToString()}, Length: {XArray.Length}";
        }

        public override string ToLongString(string format) {
            string result = ToString() + "\n";
            for (int i = 0; i < XArray.Length; i++) {
                result += $"X: {XArray[i].ToString(format)}, Y1: {YArray[2 * i].ToString(format)}, Y2: {YArray[2 * i + 1].ToString(format)}\n";
            }
            return result;
        }

        public override IEnumerator<DataItem> GetEnumerator() {
            for (int i = 0; i < XArray.Length; i++) {
                yield return new DataItem(XArray[i], YArray[2 * i], YArray[2 * i + 1]);
            }
        }

        public V1DataArray() : base(string.Empty, DateTime.Now) {
            XArray = Array.Empty<double>();
            YArray = Array.Empty<Complex>();
        }
        
        public static bool Save(string filename, V1DataArray dataArray) {
            try {
                using (StreamWriter sw = new StreamWriter(filename)) {
                    sw.WriteLine(JsonSerializer.Serialize(dataArray.Key));
                    sw.WriteLine(JsonSerializer.Serialize(dataArray.Date));
                    sw.WriteLine(JsonSerializer.Serialize(dataArray.XArray));

                    // Преобразуем массив Complex в массив ComplexNumber для сериализации
                    ComplexNumber[] complexNumbers = dataArray.YArray.Select(c => new ComplexNumber(c)).ToArray();
                    sw.WriteLine(JsonSerializer.Serialize(complexNumbers));
                }
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
                return false;
            }
        }
        public static bool Load(string filename, ref V1DataArray dataArray) {
            try {
                using (StreamReader sr = new StreamReader(filename)) {
                    string? keyLine = sr.ReadLine();
                    if (keyLine == null) {
                        Console.WriteLine("Файл поврежден или имеет неверный формат (отсутствует ключ).");
                        return false;
                    }
                    string? keyDeserialized = JsonSerializer.Deserialize<string>(keyLine);
                    if (keyDeserialized == null) {
                        Console.WriteLine("Не удалось десериализовать ключ.");
                        return false;
                    }
                    string key = keyDeserialized;

                    string? dateLine = sr.ReadLine();
                    if (dateLine == null) {
                        Console.WriteLine("Файл поврежден или имеет неверный формат (отсутствует дата).");
                        return false;
                    }
                    DateTime? dateDeserialized = JsonSerializer.Deserialize<DateTime>(dateLine);
                    if (dateDeserialized == null) {
                        Console.WriteLine("Не удалось десериализовать дату.");
                        return false;
                    }
                    DateTime date = dateDeserialized.Value;

                    string? xArrayLine = sr.ReadLine();
                    if (xArrayLine == null) {
                        Console.WriteLine("Файл поврежден или имеет неверный формат (отсутствует массив XArray).");
                        return false;
                    }
                    double[]? xArrayDeserialized = JsonSerializer.Deserialize<double[]>(xArrayLine);
                    if (xArrayDeserialized == null) {
                        Console.WriteLine("Не удалось десериализовать массив XArray.");
                        return false;
                    }
                    double[] xArray = xArrayDeserialized;

                    string? complexNumbersLine = sr.ReadLine();
                    if (complexNumbersLine == null) {
                        Console.WriteLine("Файл поврежден или имеет неверный формат (отсутствуют данные YArray).");
                        return false;
                    }
                    ComplexNumber[]? complexNumbersDeserialized = JsonSerializer.Deserialize<ComplexNumber[]>(complexNumbersLine);
                    if (complexNumbersDeserialized == null) {
                        Console.WriteLine("Не удалось десериализовать массив YArray.");
                        return false;
                    }
                    Complex[] yArray = complexNumbersDeserialized.Select(cn => cn.ToComplex()).ToArray();

                    dataArray = new V1DataArray(key, date) {
                        XArray = xArray,
                        YArray = yArray
                    };
                }
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"Ошибка при загрузке файла: {ex.Message}");
                return false;
            }
        }
    }

    public class ComplexNumber
    {
        public double Real { get; set; }
        public double Imaginary { get; set; }

        public ComplexNumber() { }

        public ComplexNumber(Complex complex)
        {
            Real = complex.Real;
            Imaginary = complex.Imaginary;
        }

        public Complex ToComplex()
        {
            return new Complex(Real, Imaginary);
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

        public override IEnumerator<DataItem> GetEnumerator() {
            foreach (var item in DataList) {
                yield return item;
            }
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
    public class V1MainCollection : List<V1Data>, IEnumerable<DataItem> {
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
        //с параметрами
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

        IEnumerator<DataItem> IEnumerable<DataItem>.GetEnumerator() {
            foreach (var data in this) {
                    foreach (var item in data) {
                        yield return item;
                    }
                }
        }    

        public IEnumerable<V1Data> MaxMeasurementData {
            get {
                if (this.Count == 0)
                    yield break;

                int maxLength = int.MinValue;
                foreach (var data in (IEnumerable<V1Data>)this) {
                    if (data.XLength > maxLength) {
                        maxLength = data.XLength;
                    }
                }

                foreach (var data in (IEnumerable<V1Data>)this) {
                    if (data.XLength == maxLength) {
                        yield return data;
                    }
                }
            }
        }
        //максимальное значение модуля первой компоненты y1
        public double MaxY1Magnitude {
            get {
                return ((IEnumerable<V1Data>)this).SelectMany(data => data)
                                            .Select(item => item.Y1.Magnitude)
                                            .DefaultIfEmpty(-1)
                                            .Max();
            }
        }

        // Свойство для перечисления координат x, встречающихся как минимум в двух элементах коллекции
        public IEnumerable<double>? DuplicateXValues {
            get {
                var duplicateX = ((IEnumerable<V1Data>)this)
                                    .SelectMany(data => data.Select(item => item.X))
                                    .GroupBy(x => x)
                                    .Where(g => g.Count() >= 2)
                                    .OrderBy(x => x.Key)
                                    .Select(x => x.Key)
                                    .Distinct();

                return duplicateX.Any() ? duplicateX : null;
            }
        }
    }

    class Program {
        static void Main(string[] args) {
            DebugFileOperations(); // Метод для отладки чтения/записи данных в файл
            DebugV1MainCollection();
        }

        
        static void DebugFileOperations() {
            Console.WriteLine("=== Отладка чтения/записи данных в файл ===");

            // Создание объекта V1DataArray с некоторыми данными
            V1DataArray originalDataArray = new V1DataArray("TestArray", DateTime.Now, new double[] { 1.0, 2.0, 3.0 }, (double x, ref Complex y1, ref Complex y2) =>
            {
                y1 = new Complex(x, x);
                y2 = new Complex(x + 1, x + 1);
            });

            Console.WriteLine("Исходный объект V1DataArray:");
            Console.WriteLine(originalDataArray.ToLongString("F2"));

            // Сохранение объекта в файл 
            string filename = "dataArray.json";
            bool saveResult = V1DataArray.Save(filename, originalDataArray);
            Console.WriteLine($"Сохранение в файл '{filename}': {(saveResult ? "Успешно" : "Не удалось")}");

            // Восстановление объекта из файла
            V1DataArray loadedDataArray = new V1DataArray("LoadedArray", DateTime.Now); // Инициализация с ключом и датой
            bool loadResult = V1DataArray.Load(filename, ref loadedDataArray);
            Console.WriteLine($"Загрузка из файла '{filename}': {(loadResult ? "Успешно" : "Не удалось")}");

            if (loadResult)
            {
                Console.WriteLine("Восстановленный объект V1DataArray:");
                Console.WriteLine(loadedDataArray.ToLongString("F2"));
            }

            Console.WriteLine();
        }


        // Метод для отладки свойств класса V1MainCollection
        static void DebugV1MainCollection()
        {
            Console.WriteLine("=== Отладка свойств класса V1MainCollection ===");
            Console.WriteLine();

            // Создание объекта V1MainCollection
            V1MainCollection mainCollection = new V1MainCollection(); //конструктор без параметров поэтоум там есть несоклько объектов уже
            //добавление элементов с пустым списком
            mainCollection.Add(new V1DataList("EmptyList", DateTime.Now));
            // Добавление V1DataArray с XLength = 0
            mainCollection.Add(new V1DataArray("EmptyArray", DateTime.Now, new double[0], (double x, ref Complex y1, ref Complex y2) =>
            {
                y1 = new Complex(0, 0);
                y2 = new Complex(0, 0);
            }));
            // Добавление других элементов для проверки
            mainCollection.Add(new V1DataArray("Array_WithData", DateTime.Now, new double[] { 4.0, 5.0 }, (double x, ref Complex y1, ref Complex y2) =>
            {
                y1 = new Complex(x, x);
                y2 = new Complex(x + 1, x + 1);
            }));
            mainCollection.Add(new V1DataList("List_WithData", DateTime.Now, new double[] { 6.0, 7.0 }, x => new DataItem(x, new Complex(x, x), new Complex(x + 1, x + 1))));

            // Вывод всей коллекции
            Console.WriteLine("Содержимое V1MainCollection:");
            Console.WriteLine(mainCollection.ToLongString("F2"));

            // Извлечение всех DataItem из коллекции
            List<DataItem> allDataItems = new List<DataItem>();
            
            foreach (var data in (IEnumerable<DataItem>)mainCollection) {
                Console.WriteLine(data);
            }

            // foreach (var data in mainCollection) {
            //     Console.WriteLine(data);
            // }


            Console.WriteLine();

            // Вызов LINQ свойств и вывод результатов

            // Пример: Максимальный модуль Y1
            Console.WriteLine("Максимальный модуль Y1:");
            Console.WriteLine(mainCollection.MaxY1Magnitude.ToString("F2"));

            // Пример: Дублирующиеся значения X
            Console.WriteLine("Дублирующиеся значения X:");
            var duplicates = mainCollection.DuplicateXValues;
            if (duplicates != null) {
                foreach (var x in duplicates) {
                    Console.WriteLine(x.ToString("F2"));
                }
            } 
            else {
                Console.WriteLine("Дублирующиеся значения X отсутствуют.");
            }
            Console.WriteLine();

            // Отладка свойства MaxMeasurementData
            Console.WriteLine("Элементы с максимальным числом результатов измерений:");
            var maxMeasurementData = mainCollection.MaxMeasurementData;
            if (maxMeasurementData != null) {
                foreach (var data in maxMeasurementData) {
                    Console.WriteLine(data.ToString());
                }
            } 
            else {
                Console.WriteLine("Элементы с максимальным числом результатов измерений отсутствуют.");
            }

            Console.WriteLine();
        }   
    }
}

