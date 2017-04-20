# EntityFrameworkCore.IncludeFilter

Modified base on EntityFrameworkCore 1.0.3

How to use:
var children = dbContext.Parent.IncludeWithFilter(p=>p.Children.Where(c=>c.Active));
var children = dbContext.Parent.IncludeWithFilter(p=>p.Children.Where(c=>c.Active))
                               .ThenIncludeWithFilter(c=>c.Items.Where(i=>i.ID > 100));

NOTE: EF still performs identity resolution, results will be overwrite on next IncludeWithFilter call
