
function Execute( )
	print( remote( "TestRemoting1", "Accept", { a = { a = "Value 1" } }, "Value 2" ) );
end

function Accept( a )
	print( a );
end