
function Accept( a, b )
	remote( "TestRemoting2", "Accept", "TestRemoting1: " .. b );
	return a.a.a;
end
