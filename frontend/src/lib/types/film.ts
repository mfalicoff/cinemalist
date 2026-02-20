export interface Film {
  title: string;
  tmdbId: string;
  country?: string;
  year?: string;
  posterUrl?: string;
  overview?: string;
  genres?: string[];
  runtime?: number;
  trailerUrl?: string;
  isInRadarr?: boolean;
  scrapedDate?: Date;
}
