using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDiskTest
{
    public static class TraunisExtension
    {
        /// <summary>
        /// Executes code on every element in a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Input collection</param>
        /// <param name="action">Action to execute on collections items</param>
        public static void Foreach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes code on every element in a collection and returns a collection containing the result of the action
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="collection">Input collection</param>
        /// <param name="func">Function to calculate result object</param>
        /// <returns>Collection of results</returns>
        public static IEnumerable<R> ForeachReturn<T, R>(this IEnumerable<T> collection, Func<T, R> func)
        {
            List<R> list = new List<R>();
            foreach (var item in collection)
            {
                list.Add(func(item));
            }
            return list;
        }

        /// <summary>
        /// Executes code on every element in a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Input collection</param>
        /// <param name="action">Action to execute on collections items</param>
        public static void Foreach<T>(this T[] collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes code on every element in a collection and returns a collection containing the result of the action
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="collection">Input collection</param>
        /// <param name="func">Function to calculate result object</param>
        /// <returns>Collection of results</returns>
        public static R[] ForeachReturn<T, R>(this T[] collection, Func<T, R> func)
        {
            R[] list = new R[collection.Length];
            for (int i = 0; i < collection.Length; i++)
            {
                list[i] = func(collection[i]);
            }
            return list;
        }

        /// <summary>
        /// Rotates an 2D-Array to the right by 90°
        /// </summary>
        /// <typeparam name="T">Type of the array</typeparam>
        /// <param name="array">Array to rotate</param>
        /// <returns>The rotated array</returns>
        public static T[,] Rotate<T>(this T[,] array)
        {
            T[,] newArray = new T[array.GetLength(1), array.GetLength(0)];

            for (int y = 0; y < array.GetLength(0); y++)
            {
                for (int x = 0; x < array.GetLength(1); x++)
                {
                    newArray[newArray.GetLength(0) - 1 - x, y] = array[y, x];
                    //else
                    //    newArray[y, newArray.GetLength(1) - y] = array[y, x];
                }
            }

            return newArray;
        }

        /// <summary>
        /// Executes the action for every value in the given 2D-Array
        /// </summary>
        /// <typeparam name="T">Type of the array</typeparam>
        /// <param name="array">Array to iterate through</param>
        /// <param name="action">Action to execute for every element. Parameters are: int x, int y, T value</param>
        public static void Foreach<T>(this T[,] array, Action<int, int, T> action)
        {
            for (int y = 0; y < array.GetLength(0); y++)
            {
                for (int x = 0; x < array.GetLength(1); x++)
                {
                    action(x, y, array[y, x]);
                }
            }
        }

        /// <summary>
        /// Executes the action for every value in the given 2D-Array
        /// </summary>
        /// <typeparam name="T">Type of the array</typeparam>
        /// <param name="array">Array to iterate through</param>
        /// <param name="action">Action to execute for every element. Parameters are: int x, int y, T value</param>
        /// <param name="selector">Determines on which values to execute the action on</param>
        public static void Foreach<T>(this T[,] array, Action<int, int, T> action, Func<T, bool> selector)
        {
            for (int y = 0; y < array.GetLength(0); y++)
            {
                for (int x = 0; x < array.GetLength(1); x++)
                {
                    if (selector(array[y, x]))
                        action(x, y, array[y, x]);
                }
            }
        }

        /// <summary>
        /// Returns the row-id of the last row where the selector returns true in a given column
        /// </summary>
        /// <typeparam name="T">Type of the array</typeparam>
        /// <param name="array">Array to evaluate</param>
        /// <param name="selector">Returns whether the value is "valid"</param>
        /// <param name="column">The column-id to search in</param>
        /// <returns>The row-id of the last "valid" row</returns>
        public static int GetLowestValueAtColumn<T>(this T[,] array, Func<T, bool> selector, int column, bool inverse = false)
        {
            if (!inverse)
            {
                for (int y = array.GetLength(0) - 1; y > 0; y--)
                {
                    if (selector(array[y, column]))
                    {
                        return y;
                    }
                }
                return 0;
            }
            else
            {
                for (int y = 0; y < array.GetLength(0); y++)
                {
                    if (selector(array[y, column]))
                    {
                        return y;
                    }
                }
                return array.GetLength(0) - 1;
            }
        }

        /// <summary>
        /// Returns the size of an 2D array as a Size object
        /// </summary>
        /// <typeparam name="T">Type of the array</typeparam>
        /// <param name="array">Array to determine the size of</param>
        /// <returns>Size of the array</returns>
        public static Size GetSize<T>(this T[,] array)
        {
            return new Size(array.GetLength(1), array.GetLength(0));
        }

        public static Point Add(this Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point Add(this Point p1, int x, int y)
        {
            return new Point(p1.X + x, p1.Y + y);
        }

        public static Point Multiply(this Point p1, Point p2)
        {
            return new Point(p1.X * p2.X, p1.Y * p2.Y);
        }

        public static Point Multiply(this Point p1, int m)
        {
            return new Point(p1.X * m, p1.Y * m);
        }

        public static void SetAsConsoleCursorPosition(this Point pos)
        {
            Console.SetCursorPosition(pos.X, pos.Y);
        }

        /// <summary>
        /// Creates an exact clone of the given object
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object to clone</param>
        /// <returns>Cloned object</returns>
        /// <exception cref="ArgumentNullException">Throws if object to clone is null</exception>
        public static T Clone<T>(this T obj) /*where T : class, new()*/
        {
            if (obj == null)
            {
                throw new ArgumentNullException("Parameter \"obj\" cannot be null.");
            }

            object[] conParams = new object[obj.GetType().GetConstructors().Length == 0 ? 0 : obj.GetType().GetConstructors()[0].GetParameters().Length];

            T clone = (T)Activator.CreateInstance(typeof(T), conParams)!;

            var propsOrig = obj.GetType().GetProperties((BindingFlags.NonPublic | BindingFlags.Public));

            var fieldsOrig = obj.GetType().GetFields((BindingFlags.NonPublic | BindingFlags.Public));

            foreach (var fields in fieldsOrig)
            {
                fields.SetValue(clone, fields.GetValue(obj));
            }

            foreach (var props in propsOrig)
            {
                if (props.SetMethod != null)
                {
                    props.SetValue(clone, props.GetValue(obj));
                }
            }

            return clone!;
        }

        /// <summary>
        /// Creates an exact clone of the given object
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object to clone</param>
        /// <returns>Cloned object</returns>
        /// <exception cref="ArgumentNullException">Throws if object to clone is null</exception>
        public static T CloneSimple<T>(this T obj) where T : class, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("Parameter \"obj\" cannot be null.");
            }

            object[] conParams = new object[obj.GetType().GetConstructors().Length == 0 ? 0 : obj.GetType().GetConstructors()[0].GetParameters().Length];

            T clone = new();

            var propsOrig = obj.GetType().GetProperties((BindingFlags.NonPublic | BindingFlags.Public) & BindingFlags.Instance);

            var fieldsOrig = obj.GetType().GetFields((BindingFlags.NonPublic | BindingFlags.Public) & BindingFlags.Instance);

            foreach (var fields in fieldsOrig)
            {
                fields.SetValue(clone, fields.GetValue(obj));
            }

            foreach (var props in propsOrig)
            {
                if (props.SetMethod != null)
                {
                    props.SetValue(clone, props.GetValue(obj));
                }
            }

            return clone!;
        }
    }
}
