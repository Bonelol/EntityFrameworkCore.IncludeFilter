# EntityFrameworkCore.IncludeFilter

Modified base on EntityFrameworkCore 2.1.0


How to use:

```csharp
public void ConfigureServices(IServiceCollection services)
{ 
  ...

  services.AddDbContext<DbContext>(options => options.UseSqlServer("connection_string").AddIncludeWithFilterMethods());
  
  ...
}
``` 

```csharp
var children = dbContext.Parent.IncludeWithFilter(p=>p.Children, c=>c.Active)
                               .ThenIncludeWithFilter(c=>c.Items, i=>i.ID > 100);
```                               

NOTE: EF still performs identity resolution, results will be overwrite on next IncludeWithFilter call
