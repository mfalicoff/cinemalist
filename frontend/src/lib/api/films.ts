import { PUBLIC_API_URL } from "$env/static/public";
import type { Film } from "$lib/types/film";

export async function fetchAllFilms(): Promise<Film[]> {
  try {
    const response = await fetch(`${PUBLIC_API_URL}/api/films/all`);

    if (!response.ok) {
      throw new Error(`Failed to fetch films: ${response.statusText}`);
    }

    const films = (await response.json()).result;
    return films;
  } catch (error) {
    console.error("Error fetching films:", error);
    throw error;
  }
}

export async function fetchFilmById(imdbId: string): Promise<Film | null> {
  try {
    const response = await fetch(`${PUBLIC_API_URL}/api/films/${imdbId}`);

    if (!response.ok) {
      throw new Error(`Failed to fetch film ${imdbId}: ${response.statusText}`);
    }

    const films = (await response.json()).result;
    // API returns array, get first item
    return films.length > 0 ? films[0] : null;
  } catch (error) {
    console.error(`Error fetching film ${imdbId}:`, error);
    throw error;
  }
}
