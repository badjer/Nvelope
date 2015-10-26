using System;
using System.Linq;
using System.Text.RegularExpressions;
#if !PCL
using System.Collections.Specialized;
using System.Data;
#endif
using Nvelope.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Nvelope
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Tests whether 2 objects are equal, but handles nulls gracefully
        /// </summary>
        /// <remarks>Stolen from Clojure</remarks>
        public static bool Eq(this object obj, object other)
        {
            // Handle nulls
            if (obj == null)
                return (other == null);

            if (other == null)
                return false;

            return obj.Equals(other);
        }

        /// <summary>
        /// Inverse of Eq
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Neq(this object obj, object other)
        {
            return !obj.Eq(other);
        }

        /// <summary>
        /// Try to call CompareTo on 2 objects - do type conversion if necessary
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="other"></param>
        /// <param name="throwOnUncomparable">Throw an exception if the objects can't be compared?</param>
        /// <returns>null if only one arg is null, or if objects aren't comparable. Else the value of obj.CompareTo(other)</returns>
        public static int? LazyCompare(this object obj, object other, bool throwOnUncomparable = false)
        {
            // Handle nulls
            if (obj == null && other == null)
                return 0;

            if (other == null || obj == null)
                return null;

            var typeObj = obj.GetType();
            var typeOther = other.GetType();

            // This is a bit involved.
            // We want to find an implementation of IComparable on one of the objects that we can use to
            // do the comparison. We'll take anything we can get, but we'd prefer to NOT use string.CompareTo
            // if possible - everything can be converted to string, but not always in a meaningful way, so that
            // means that doing a string compare on the objects is not ideal.
            if (typeObj == typeOther && obj is IComparable)
                return (obj as IComparable).CompareTo(other);
            else if (!(obj is string) && obj is IComparable && other.CanConvertTo(typeObj))
                return (obj as IComparable).CompareTo(other.ConvertTo(typeObj));
            else if (!(other is string) && other is IComparable && obj.CanConvertTo(typeOther))
                return -1 * (other as IComparable).CompareTo(obj.ConvertTo(typeOther));
            else if (obj is IComparable && other.CanConvertTo(typeObj))
                return (obj as IComparable).CompareTo(other.ConvertTo(typeObj));
            else if (other is IComparable && obj.CanConvertTo(typeOther))
                return -1 * (other as IComparable).CompareTo(obj.ConvertTo(typeOther));


            if (throwOnUncomparable)
                throw new InvalidOperationException("Could not compare the two objects: " + obj + " and " + other);

            return null;
        }

        /// <summary>
        /// Try to figure out if obj is &lt; other - do type conversion if necessary
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="other"></param>
        /// <param name="throwOnUncomparable">Throw an exception if the values can't be compared?</param>
        /// <returns>null if one of the args is null, or if the args can't be compared. Else the value of obj.CompareTo(other)</returns>
        public static bool? LazyLt(this object obj, object other, bool throwOnUncomparable = false)
        {
            var compRes = LazyCompare(obj, other, throwOnUncomparable);
            if (compRes == null)
                return null;
            return compRes.Value < 0;
        }

        /// <summary>
        /// Try to figure out if obj is &lt;= other - do type conversion if necessary
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="other"></param>
        /// <param name="throwOnUncomparable">Throw an exception if the values can't be compared?</param>
        /// <returns>null if one of the args is null, or if the args can't be compared. Else the value of obj.CompareTo(other)</returns>
        public static bool? LazyLtEq(this object obj, object other, bool throwOnUncomparable = false)
        {
            var compRes = LazyCompare(obj, other, throwOnUncomparable);
            if (compRes == null)
                return null;
            return compRes.Value <= 0;
        }

        /// <summary>
        /// Try to figure out if obj is &gt; other - do type conversion if necessary
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="other"></param>
        /// <param name="throwOnUncomparable">Throw an exception if the values can't be compared?</param>
        /// <returns>null if one of the args is null, or if the args can't be compared. Else the value of obj.CompareTo(other)</returns>
        public static bool? LazyGt(this object obj, object other, bool throwOnUncomparable = false)
        {
            var compRes = LazyCompare(obj, other, throwOnUncomparable);
            if (compRes == null)
                return null;
            return compRes.Value > 0;
        }

        /// <summary>
        /// Try to figure out if obj is &gt;= other - do type conversion if necessary
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="other"></param>
        /// <param name="throwOnUncomparable">Throw an exception if the values can't be compared?</param>
        /// <returns>null if one of the args is null, or if the args can't be compared. Else the value of obj.CompareTo(other)</returns>
        public static bool? LazyGtEq(this object obj, object other, bool throwOnUncomparable = false)
        {
            var compRes = LazyCompare(obj, other, throwOnUncomparable);
            if (compRes == null)
                return null;
            return compRes.Value >= 0;
        }

        public static bool LazyEq(this object obj, object other)
        {
            // Handle nulls
            if (obj == null)
                return (other == null);

            if (other == null)
                return false;

            var typeObj = obj.GetType();
            var typeOther = other.GetType();

            if (typeObj == typeOther)
                return obj.Eq(other);

            if (obj.CanConvertTo(typeOther))
                return obj.ConvertTo(typeOther).Eq(other);

            if (other.CanConvertTo(typeObj))
                return obj.Eq(other.ConvertTo(typeObj));

            throw new InvalidOperationException("Could not compare the two objects: " + obj + " and " + other);
        }

        public static bool LazyNeq(this object obj, object other)
        {
            return !obj.LazyEq(other);
        }

        /// <summary>
        /// Like ToString, but it handles nulls and gives nicer results for
        /// some objects.
        /// </summary>
        /// <remarks>There should be no other implementations of Print, because we want the
        /// thing to behave polymorphically, and there's no assurance of that unless
        /// we centralize here</remarks>
        public static string Print(this object o)
        {
            // This function should also work polymorphically
            // Sometimes, we've got variables of type object, but we want them to print 
            // nicer than ToString() for their type (ie, for decimals, ToString() works stupidly)
            // So we do shotgun polymorphism here to take care of that, since we can't 
            // hack into the original types to override their ToString methods

            if (o == null)
                return o.ToStringN();

            var type = o.GetType();

            if (type == typeof(decimal))
                return ((decimal)o).PrintDecimal(); // Decimals don't do ToString in a reasonable way
            else if (type == typeof(string))
                return o.ToStringN(); // This is to prevent the compiler from calling the IEnumerable<char> verison for strings
#if !PCL
            else if (type == typeof(NameValueCollection))
                return ((NameValueCollection)o).ToDictionary().Print();
#endif
            else if (type.Implements<IDictionary>())
                return ((IDictionary)o).PrintDict();
            else if (type.Implements<IEnumerable>())
                return "(" + ((IEnumerable)o).ToIEnumerableObj().Select(t => t.Print()).Join(",") + ")";
            else if (o is Match)
            {
                var groups = ((Match)o).Groups.ToList().ToList();
                return groups.Select(g => g.ToString()).Print();

                // We can't do this, because the first group of a Match might be
                // the Match itself - that would get us into an infinite loop
                //return ((Match)o).Groups.ToList().Print(); // Regex group
            }
            else if (o is Capture)
                return ((Capture)o).Value; // Regex capture

#if !PCL
            else if (o is DataTable)
                return "(" + ((DataTable)o).Rows.ToList().Select(l => l.Print()).Join(",") + ")";
            else if (o is DataRow)
                return ((DataRow)o).ToDictionary().Print();
#endif
            else
                return o.ToStringN();
        }
    }
}