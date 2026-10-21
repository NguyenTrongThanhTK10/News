const dateFormatter = new Intl.DateTimeFormat('vi-VN', {
  day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh',
});

export function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : dateFormatter.format(date);
}

export function categoryIcon(slug: string): string {
  return ({
    'doi-song': 'pi-heart', 'cong-nghe': 'pi-microchip', 'du-lich': 'pi-compass',
    'van-hoa': 'pi-palette', 'giao-duc': 'pi-book', 'the-thao': 'pi-trophy',
  } as Record<string, string>)[slug] || 'pi-th-large';
}
