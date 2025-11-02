export interface Film {
  title: string;
  imdbId: string;
  tmdbId: string;
  country?: string;
  year?: string;
  posterUrl?: string;
  isInRadarr?: boolean;
}
