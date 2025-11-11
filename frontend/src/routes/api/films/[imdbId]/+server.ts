import type { RequestHandler } from './$types';
import { fetchFilmById } from '$lib/api/films';
import { json } from '@sveltejs/kit';

export const GET: RequestHandler = async ({ params }) => {
  const { imdbId } = params;

  try {
    const film = await fetchFilmById(imdbId);

    if (!film) {
      return json({ error: 'Film not found' }, { status: 404 });
    }

    return json(film);
  } catch (error) {
    console.error(`Error fetching film ${imdbId}:`, error);
    return json({ error: 'Failed to fetch film' }, { status: 500 });
  }
};
