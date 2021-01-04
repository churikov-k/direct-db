#### A small C# service to work directly with a database  

Sometimes we need to use a database without any ORM frameworks like EF.  
Here is a simple service that allows you to work with a database directly through `System.Data.SqlClient`.  

It are available three possibilities:  
- `ExecuteNonQuery`
- `ExecuteScalar`
- `GetListFromDb`

A pack of queries with `GO` between them is supported.  

