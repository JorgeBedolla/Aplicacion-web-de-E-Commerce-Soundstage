function WSAdmin(url)
{
	this.url = url;

	this.postJson = function(operacion,args,callback)
	{
		try
		{
			var body = JSON.stringify(args);
			var request = new XMLHttpRequest();
			request.open("POST" ,url + "/" + operacion);
			request.setRequestHeader("Content-Type","application/json");
			request.responseType = 'json';
			request.onload = function()
			{
				if (callback != null) callback(request.status,request.response);
			};
			request.send(body);
		}
		catch (e)
		{
			alert("Error: " + e.message);
		}
	}

	this.getJson = function(operacion,args,callback)
	{
		try
		{
			var body = JSON.stringify(args);
			var request = new XMLHttpRequest();
			request.open("GET" ,url + "/" + operacion);
			request.setRequestHeader("Content-Type","application/json");
			request.responseType = 'json';
			request.onload = function()
			{
				if (callback != null) callback(request.status,request.response);
			};
			request.send(body);
		}
		catch (e)
		{
			alert("Error: " + e.message);
		}
	}

	this.deleteJson = function(operacion,args,callback)
	{
		try
		{
			var body = JSON.stringify(args);
			var request = new XMLHttpRequest();
			request.open("DELETE" ,url + "/" + operacion);
			request.setRequestHeader("Content-Type","application/json");
			request.responseType = 'json';
			request.onload = function()
			{
				if (callback != null) callback(request.status,request.response);
			};
			request.send(body);
		}
		catch (e)
		{
			alert("Error: " + e.message);
		}
	}
}