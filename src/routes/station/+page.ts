import type { PageLoad } from '$types';

export const load: PageLoad = ({ url }) => {
	const stationId = url.searchParams.get('stationId');
	const date = url.searchParams.get('date');
	console.log(stationId);
	return {
	  stationId,date
	};
  };