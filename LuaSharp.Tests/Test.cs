// 
// Test.cs
//  
// Author:
//       Jonathan Dickinson <jonathan@dickinsons.co.za>
// 
// Copyright (c) 2011 Copyright (c) 2011 Grounded Games
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
using NUnit.Framework;

namespace LuaSharp.Tests
{
	[TestFixture()]
	public class Test : LuaTest
	{
		[Test()]
		public void TestExecution ()
		{
			AssertOutput( LuaScripts.GetScriptString( "TestExecution" ), "Execute", "Executed", "Basic execution." );
		}
		
		[Test()]
		public void TestRemoting ()
		{
			using( Lua remoting1 = new Lua( ) )
			using( Lua remoting2 = new Lua( ) )
			{
				remoting1["remote"] = RemoteFunction.Instance;
				
				LookupTable<string, Lua>.Store( "TestRemoting1", remoting1 );
				LookupTable<string, Lua>.Store( "TestRemoting2", remoting2 );
				
				remoting1.DoString( LuaScripts.GetScriptString( "TestRemoting1" ) );
				remoting2.DoString( LuaScripts.GetScriptString( "TestRemoting2" ) );
				
				AssertOutput( remoting2, "Execute", "TestRemoting1: Value 2\nValue 1" );
			}
		}
	}
}

