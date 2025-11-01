import type { PageServerLoad } from "./$types";
import { fetchAllFilms } from "$lib/api/films";

export const load: PageServerLoad = async () => {
  try {
    const films = await fetchAllFilms();
    return {
      films,
    };
  } catch (error) {
    console.error("Error loading films:", error);
    return {
      films: [],
      error: "Failed to load films from the backend",
    };
  }
};
