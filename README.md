# EntityFrameworkCore.IncludeFilter

Modified base on EntityFrameworkCore 1.0.3

How to use:
var children = dbContext.Parent.IncludeWithFilter(p=>p.Children.Where(c=>c.Active));

NOTE: EF still performs identity resolution, results will be overwrite on next IncludeWithFilter call
