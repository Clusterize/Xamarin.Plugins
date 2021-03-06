﻿
using System;

#if __UNIFIED__
using Foundation;

#else
using MonoTouch.Foundation;
#endif
using Refractored.Xam.Settings.Abstractions;

namespace Refractored.Xam.Settings
{
	/// <summary>
	/// Main implementation for ISettings
	/// </summary>
	public class Settings : ISettings
	{
		private Func<string, Type, object> deserializationFunc;
		private Func<object, string> serializationFunc;

		private readonly object locker = new object ();

		/// <summary>
		/// Optional. Call this to enable serialization and deserialization of natively unsupported types.
		/// </summary>
		/// <param name="serializationFunc">Function to be used for serialization.</param>
		/// <param name="deserializationFunc">Function to be used for deserialization</param>
		public void Initialize (Func<object, string> serializationFunc, Func<string, Type, object> deserializationFunc)
		{
			this.serializationFunc = serializationFunc;
			this.deserializationFunc = deserializationFunc;
		}

		/// <summary>
		/// Gets the current value or the default that you specify.
		/// </summary>
		/// <typeparam name="T">Vaue of t (bool, int, float, long, string)</typeparam>
		/// <param name="key">Key for settings</param>
		/// <param name="defaultValue">default value if not set</param>
		/// <returns>Value or default</returns>
		public T GetValueOrDefault<T> (string key, T defaultValue = default(T))
		{
			lock (locker) {
				var defaults = NSUserDefaults.StandardUserDefaults;
        
				if (defaults.ValueForKey (new NSString (key)) == null)
					return defaultValue;

				Type typeOf = typeof(T);
				if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition () == typeof(Nullable<>)) {
					typeOf = Nullable.GetUnderlyingType (typeOf);
				}
				object value = null;
				var typeCode = Type.GetTypeCode (typeOf);
				switch (typeCode) {
				case TypeCode.Decimal:
					var savedDecimal = defaults.StringForKey (key);
					value = Convert.ToDecimal (savedDecimal, System.Globalization.CultureInfo.InvariantCulture);
					break;
				case TypeCode.Boolean:
					value = defaults.BoolForKey (key);
					break;
				case TypeCode.Int64:
					var savedInt64 = defaults.StringForKey (key);
					value = Convert.ToInt64 (savedInt64, System.Globalization.CultureInfo.InvariantCulture);
					break;
				case TypeCode.Double:
					value = defaults.DoubleForKey (key);
					break;
				case TypeCode.String:
					value = defaults.StringForKey (key);
					break;
				case TypeCode.Int32:
#if __UNIFIED__
            value = (Int32)defaults.IntForKey(key);
#else
					value = defaults.IntForKey (key);
#endif
					break;
				case TypeCode.Single:
#if __UNIFIED__
            value = (float)defaults.FloatForKey(key);
#else
					value = defaults.FloatForKey (key);
#endif
					break;

				case TypeCode.DateTime:
					var savedTime = defaults.StringForKey (key);
					var ticks = string.IsNullOrWhiteSpace (savedTime) ? -1 : Convert.ToInt64 (savedTime, System.Globalization.CultureInfo.InvariantCulture);
					if (ticks == -1)
						value = defaultValue;
					else
						value = new DateTime (ticks);
					break;
				default:

					if (defaultValue is Guid) {
						var outGuid = Guid.Empty;
						var savedGuid = defaults.StringForKey (key);
						if (string.IsNullOrWhiteSpace (savedGuid)) {
							value = outGuid;
						} else {
							Guid.TryParse (savedGuid, out outGuid);
							value = outGuid;
						}
					} else if (this.deserializationFunc != null) {
						try
						{
							var savedGeneric = defaults.StringForKey (key);
							value = this.deserializationFunc (savedGeneric, typeOf);
						}
						catch
						{
							value = null;
						}
					} else {
						throw new ArgumentException (string.Format ("Value of type {0} is not supported. To enable generic deserialization, use ISettings.Initialize.", value.GetType ().Name));
					}

					break;
				}


				return null != value ? (T)value : defaultValue;
			}
		}

		/// <summary>
		/// Adds or updates a value
		/// </summary>
		/// <param name="key">key to update</param>
		/// <param name="value">value to set</param>
		/// <returns>True if added or update and you need to save</returns>
		public bool AddOrUpdateValue<T> (string key, T value)
		{
			Type typeOf = typeof(T);
			if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition () == typeof(Nullable<>)) {
				typeOf = Nullable.GetUnderlyingType (typeOf);
			}
			var typeCode = Type.GetTypeCode (typeOf);
			return AddOrUpdateValue (key, value, typeCode);
		}

		/// <summary>
		/// Adds or updates the value 
		/// </summary>
		/// <param name="key">Key for settting</param>
		/// <param name="value">Value to set</param>
		/// <returns>True of was added or updated and you need to save it.</returns>
		/// <exception cref="NullReferenceException">If value is null, this will be thrown.</exception>
		[Obsolete ("This method is now obsolete, please use generic version as this may be removed in the future.")]
		public bool AddOrUpdateValue (string key, object value)
		{
			Type typeOf = value.GetType ();
			if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition () == typeof(Nullable<>)) {
				typeOf = Nullable.GetUnderlyingType (typeOf);
			}
			var typeCode = Type.GetTypeCode (typeOf);
			return AddOrUpdateValue (key, value, typeCode);   
		}

		private bool AddOrUpdateValue (string key, object value, TypeCode typeCode)
		{
			lock (locker) {
				var defaults = NSUserDefaults.StandardUserDefaults;
				switch (typeCode) {
				case TypeCode.Decimal:

					defaults.SetString (Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture), key);
					break;
				case TypeCode.Boolean:
					defaults.SetBool (Convert.ToBoolean (value), key);
					break;
				case TypeCode.Int64:
					defaults.SetString (Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture), key);
					break;
				case TypeCode.Double:
					defaults.SetDouble (Convert.ToDouble (value, System.Globalization.CultureInfo.InvariantCulture), key);
					break;
				case TypeCode.String:
					defaults.SetString (Convert.ToString (value), key);
					break;
				case TypeCode.Int32:
					defaults.SetInt (Convert.ToInt32 (value, System.Globalization.CultureInfo.InvariantCulture), key);
					break;
				case TypeCode.Single:
					defaults.SetFloat (Convert.ToSingle (value, System.Globalization.CultureInfo.InvariantCulture), key);
					break;
				case TypeCode.DateTime:
					defaults.SetString (Convert.ToString ((Convert.ToDateTime (value)).Ticks), key);
					break;
				default:
					if (value is Guid) {
						if (value == null)
							value = Guid.Empty;

						defaults.SetString (((Guid)value).ToString (), key);
					} else if (this.serializationFunc != null) {
						defaults.SetString (this.serializationFunc (value), key);
					} else {
						throw new ArgumentException (string.Format ("Value of type {0} is not supported. To enable string serialization, use ISettings.Initialize.", value.GetType ().Name));
					}
					break;
				}
				try {
					defaults.Synchronize ();
				} catch (Exception ex) {
					Console.WriteLine ("Unable to save: " + key, " Message: " + ex.Message);
				}
			}

     
			return true;
		}

		/// <summary>
		/// Saves all currents settings outs.
		/// </summary>
		[Obsolete ("Save is deprecated and settings are automatically saved when AddOrUpdateValue is called.")]
		public void Save ()
		{
     
		}

		/// <summary>
		/// Removes a desired key from the settings
		/// </summary>
		/// <param name="key">Key for setting</param>
		public void Remove (string key)
		{
			lock (locker) {
				var defaults = NSUserDefaults.StandardUserDefaults;
				try {
					var nsString = new NSString (key);
					if (defaults.ValueForKey (nsString) != null) {
						defaults.RemoveObject (key);
						defaults.Synchronize ();
					}
				} catch (Exception ex) {
					Console.WriteLine ("Unable to remove: " + key, " Message: " + ex.Message);
				}
			}
		}

		public void Clear()
		{
			NSUserDefaults.StandardUserDefaults.RemovePersistentDomain(NSBundle.MainBundle.BundleIdentifier);
		}
	}
}