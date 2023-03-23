﻿using System.Globalization;

using CsvHelper.Configuration;

using VerticalSliceArchitecture.Application.Features.TodoLists;

namespace VerticalSliceArchitecture.Application.Infrastructure.Files.Maps;

public class TodoItemRecordMap : ClassMap<TodoItemRecord>
{
    public TodoItemRecordMap()
    {
        AutoMap(CultureInfo.InvariantCulture);

        Map(m => m.Done).ConvertUsing(c => c.Done ? "Yes" : "No");
    }
}
