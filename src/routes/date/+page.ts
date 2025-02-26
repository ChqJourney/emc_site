import type { PageLoad } from './$types';

export const load: PageLoad = ({ url }) => {
	const date = url.searchParams.get('date');
	console.log(date);
	return {
	  date
	};
  };