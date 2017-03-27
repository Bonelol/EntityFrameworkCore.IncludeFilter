# EntityFrameworkCore.IncludeFilter

Modified base on EntityFrameworkCore 1.0.3
In EntityFrameworkCore 1.1, there should be changed some but will be more easier.

How to use:
var children = dbContext.Parent.IncludeWithFilter(p=>p.Children.Where(c=>c.Active));

NOTE: EF still performs identity resolution, results will be overwrite on next IncludeWithFilter call
