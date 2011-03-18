// 
// LookupTable.cs
//  
// Author:
//       Jonathan Dickinson <jonathan@dickinsons.co.za>
// 
// Copyright (c) 2011 Jonathan Dickinson
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Threading;

namespace LuaSharp
{
	/// <summary>
	/// Represents a lookup table for objects.
	/// </summary>
	public static class LookupTable<TKey, TValue> 
		where TValue : class
	{
		private static Dictionary<TKey, WeakReference> values = new Dictionary<TKey, WeakReference>( );
		private static ReaderWriterLockSlim valuesLock = new ReaderWriterLockSlim( );

		/// <summary>
		/// Stores the specified values according to its key.
		/// </summary>
		/// <param name='key'>
		/// The key for the value.
		/// </param>
		/// <param name='value'>
		/// The value to store.
		/// </param>
		public static void Store( TKey key, TValue value )
		{
			valuesLock.EnterWriteLock();
			try
			{
				values.Remove( key );
				if( value != null )
					values.Add( key, new WeakReference( value ) );
			}
			finally
			{
				valuesLock.ExitWriteLock( );
			}
		}
			
		/// <summary>
		/// Remove the value associated with the specified key from the table.
		/// </summary>
		/// <param name='key'>
		/// The key to lookup and remove.
		/// </param>
		public static void Remove( TKey key )
		{
			Store( key, null );
		}
			
		/// <summary>
		/// Retrieves a value from the lookup table. 
		/// </summary>
		/// <param name='key'>
		/// The key of the value to look up.
		/// </param>
		/// <param name='value'>
		/// A container for the resulting value.
		/// </param>
		/// <remarks>
		/// A value indicating whether the value was found and was alive.
		/// </remarks>
		public static bool Retrieve( TKey key, out TValue value )
		{
			valuesLock.EnterReadLock();
			try
			{
				WeakReference reference;
				if( values.TryGetValue( key, out reference ) )
				{
					value = (TValue) reference.Target;
					if( reference.IsAlive )
					{
						return true;
					}
					
					// We allow this to fall through so that we can gain write access.
				}
				else
				{
					value = null;
					return false;
				}
			}
			finally
			{
				valuesLock.ExitReadLock( );
			}
			
			// Unfortunately we need to check everything again.
			valuesLock.EnterWriteLock();
			try
			{
				WeakReference reference;
				if( values.TryGetValue( key, out reference ) )
				{
					value = (TValue) reference.Target;
					if( reference.IsAlive )
					{
						return true;
					}
					
					value = null;
					values.Remove( key );
					return false;
				}
				else
				{
					value = null;
					return false;
				}
			}
			finally
			{
				valuesLock.ExitWriteLock( );
			}
		}
	}
}

