export enum LibraryType {
    Manga = 0,
    Comic = 1,
    Book = 2,
}

export interface Library {
    id: number;
    name: string;
    lastScanned: string;
    type: LibraryType;
    folders: string[];
    coverImage?: string | null;
    folderWatching: boolean;
    includeInDashboard: boolean;
    includeInRecommended: boolean;
    includeInSearch: boolean;
    manageCollections: boolean;
    manageReadingLists: boolean;
    allowScrobbling: boolean;
    collapseSeriesRelationships: boolean;
}
