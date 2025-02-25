﻿using System;
using System.Collections.Generic;
using System.Globalization;
using API.Entities;
using API.Entities.Enums;
using API.Services.Tasks.Scanner.Parser;

namespace API.Helpers.Builders;

public class ChapterBuilder : IEntityBuilder<Chapter>
{
    private readonly Chapter _chapter;
    public Chapter Build() => _chapter;

    public ChapterBuilder(string number, string? range=null)
    {
        _chapter = new Chapter()
        {
            Range = string.IsNullOrEmpty(range) ? number : range,
            Title = string.IsNullOrEmpty(range) ? number : range,
            Number = Parser.MinNumberFromRange(number).ToString(CultureInfo.InvariantCulture),
            Files = new List<MangaFile>(),
            Pages = 1
        };
    }

    public static ChapterBuilder FromParserInfo(ParserInfo info)
    {
        var specialTreatment = info.IsSpecialInfo();
        var specialTitle = specialTreatment ? info.Filename : info.Chapters;
        var builder = new ChapterBuilder(Services.Tasks.Scanner.Parser.Parser.DefaultChapter);
        return builder.WithNumber(specialTreatment ? Services.Tasks.Scanner.Parser.Parser.DefaultChapter : Services.Tasks.Scanner.Parser.Parser.MinNumberFromRange(info.Chapters) + string.Empty)
            .WithRange(specialTreatment ? info.Filename : info.Chapters)
            .WithTitle((specialTreatment && info.Format == MangaFormat.Epub)
            ? info.Title
            : specialTitle)
            .WithIsSpecial(specialTreatment);
    }

    public ChapterBuilder WithId(int id)
    {
        _chapter.Id = Math.Max(id, 0);
        return this;
    }

    public ChapterBuilder WithNumber(string number)
    {
        _chapter.Number = number;
        return this;
    }

    public ChapterBuilder WithStoryArc(string arc)
    {
        _chapter.StoryArc = arc;
        return this;
    }

    public ChapterBuilder WithStoryArcNumber(string number)
    {
        _chapter.StoryArcNumber = number;
        return this;
    }

    private ChapterBuilder WithRange(string range)
    {
        _chapter.Range = range;
        return this;
    }

    public ChapterBuilder WithReleaseDate(DateTime time)
    {
        _chapter.ReleaseDate = time;
        return this;
    }

    public ChapterBuilder WithAgeRating(AgeRating rating)
    {
        _chapter.AgeRating = rating;
        return this;
    }

    public ChapterBuilder WithPages(int pages)
    {
        _chapter.Pages = pages;
        return this;
    }
    public ChapterBuilder WithCoverImage(string cover)
    {
        _chapter.CoverImage = cover;
        return this;
    }
    public ChapterBuilder WithIsSpecial(bool isSpecial)
    {
        _chapter.IsSpecial = isSpecial;
        return this;
    }
    public ChapterBuilder WithTitle(string title)
    {
        _chapter.Title = title;
        return this;
    }

    public ChapterBuilder WithFile(MangaFile file)
    {
        _chapter.Files ??= new List<MangaFile>();
        _chapter.Files.Add(file);
        return this;
    }

    public ChapterBuilder WithFiles(IList<MangaFile> files)
    {
        _chapter.Files = files ?? new List<MangaFile>();
        return this;
    }

    public ChapterBuilder WithLastModified(DateTime lastModified)
    {
        _chapter.LastModified = lastModified;
        _chapter.LastModifiedUtc = lastModified.ToUniversalTime();
        return this;
    }

    public ChapterBuilder WithCreated(DateTime created)
    {
        _chapter.Created = created;
        _chapter.CreatedUtc = created.ToUniversalTime();
        return this;
    }
}
