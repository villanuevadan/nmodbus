using System;
using System.Collections.ObjectModel;
using System.Linq;
using Modbus.Utility;

namespace Modbus.Data
{
	/// <summary>
	/// Object simulation of device memory map.
	/// </summary>
	public class DataStore
	{
		/// <summary>
		/// Represents the method that will handle the DataStoreReadFrom and DataStoreWrittenTo events of a DataStore object.
		/// </summary>
		public delegate void DataStoreReadWriteEventHandler(object sender, DataStoreEventArgs e);

		/// <summary>
		/// Occurs when the DataStore is written to via a Modbus command.
		/// </summary>
		public event DataStoreReadWriteEventHandler DataStoreReadFrom;

		/// <summary>
		/// Occurs when the DataStore is read from via a Modbus command.
		/// </summary>
		public event DataStoreReadWriteEventHandler DataStoreWrittenTo;

		/// <summary>
		/// Initializes a new instance of the <see cref="DataStore"/> class.
		/// </summary>
		public DataStore()
		{
			CoilDiscretes = new ModbusDataCollection<bool> { ModbusDataType = ModbusDataType.Coil };
			InputDiscretes = new ModbusDataCollection<bool> { ModbusDataType = ModbusDataType.Input };
			HoldingRegisters = new ModbusDataCollection<ushort> { ModbusDataType = ModbusDataType.HoldingRegister };
			InputRegisters = new ModbusDataCollection<ushort> { ModbusDataType = ModbusDataType.InputRegister };
		}

		/// <summary>
		/// Gets the coil discretes.
		/// </summary>
		public ModbusDataCollection<bool> CoilDiscretes{ get; private set; }

		/// <summary>
		/// Gets the input discretes.
		/// </summary>
		public ModbusDataCollection<bool> InputDiscretes { get; private set; }

		/// <summary>
		/// Gets the holding registers.
		/// </summary>
		public ModbusDataCollection<ushort> HoldingRegisters { get; private set; }

		/// <summary>
		/// Gets the input registers.
		/// </summary>
		public ModbusDataCollection<ushort> InputRegisters { get; private set; }

		/// <summary>
		/// Retrieves subset of data from collection.
		/// </summary>
		/// <typeparam name="T">The collection type.</typeparam>
		/// <typeparam name="U">The type of elements in the collection.</typeparam>
		internal static T ReadData<T, U>(DataStore dataStore, ModbusDataCollection<U> dataSource, ushort startAddress, ushort count) where T : Collection<U>, new()
		{
			int startIndex = startAddress + 1;

			if (startIndex < 0 || startIndex >= dataSource.Count)
				throw new ArgumentOutOfRangeException("Start address was out of range. Must be non-negative and <= the size of the collection.");

			if (dataSource.Count < startIndex + count)
				throw new ArgumentOutOfRangeException("Read is outside valid range.");

			U[] dataToRetrieve = dataSource.Slice(startIndex, count).ToArray();
			T result = new T();

			for (int i = 0; i < count; i++)
				result.Add(dataToRetrieve[i]);

			dataStore.DataStoreReadFrom.IfNotNull(e => e(dataStore, DataStoreEventArgs.CreateDataStoreEventArgs(startAddress, dataSource.ModbusDataType, result)));

			return result;
		}

		/// <summary>
		/// Write data to data store.
		/// </summary>
		/// <typeparam name="TData">The type of the data.</typeparam>
		internal static void WriteData<TData>(DataStore dataStore, Collection<TData> items, ModbusDataCollection<TData> destination, ushort startAddress)
		{
			int startIndex = startAddress + 1;

			if (startIndex < 0 || startIndex >= destination.Count)
				throw new ArgumentOutOfRangeException("Start address was out of range. Must be non-negative and <= the size of the collection.");
			
			if (destination.Count < startIndex + items.Count)
				throw new ArgumentOutOfRangeException("Items collection is too large to write at specified start index.");

			CollectionUtility.Update(items, destination, startIndex);

			dataStore.DataStoreWrittenTo.IfNotNull(e => e(dataStore, DataStoreEventArgs.CreateDataStoreEventArgs(startAddress, destination.ModbusDataType, items)));
		}
	}
}