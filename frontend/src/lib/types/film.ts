export interface Film {
  title: string;
  tmdbId: string;
  country?: string;
  year?: string;
  posterUrl?: string;
  isInRadarr?: boolean;
  scrapedDate?: Date;
}
