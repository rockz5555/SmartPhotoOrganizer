﻿namespace SmartPhotoOrganizer.InputRelated
{
    public enum UserAction
    {
        None = 1,
        ShowHelp = 2,
        ShowOptions = 3,
        ReloadFilesFromDisk = 4,
        ToggleFullscreen = 5,
        Minimize = 6,
        Quit = 7,
        RateAs1 = 8,
        RateAs2 = 9,
        RateAs3 = 10,
        RateAs4 = 11,
        RateAs5 = 12,
        ClearRating = 13,
        Tag = 14,
        TagEditMultiple = 15,
        TagRenameOrDelete = 40,
        Rename = 16,
        Move = 17,
        CopyFiles = 42,
        DeleteCurrentFile = 18,
        ShowPreviousImage = 21,
        ShowNextImage = 22,
        MoveToFirstImage = 23,
        MoveToLastImage = 24,
        PlayStopSlideshow = 41,
        ShowLists = 25,
        ChangeOrder = 26,
        Search = 27,
        ClearSearch = 28,
        ShowRating1OrGreater = 29,
        ShowRating2OrGreater = 30,
        ShowRating3OrGreater = 31,
        ShowRating4OrGreater = 32,
        ShowRating5OrGreater = 33,
        ClearRatingFilter = 34,
        ShowOnlyUnrated = 35,
        ShowOnlyUntagged = 36,
    }
}