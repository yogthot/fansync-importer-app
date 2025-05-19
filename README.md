# Fansync Importer App
Pledge importer for https://fansync.moe/

## Documentation
All this application does is get plan and supporter info from fanbox apis and then sends it to the corresponding fansync api.
It has a few extra features to make it *just work*, but that's about it.

If you're looking to implement something yourself, here's a barebones example of what the app does every hour.
```python
import os
import requests
import json
import re

FANBOX_COOKIE = os.environ['FANBOX_COOKIE']
PIXIV_ID = os.environ['PIXIV_ID']
FANSYNC_TOKEN = os.environ['FANSYNC_TOKEN']


CREATOR_URL_REGEXP = re.compile(r'https?:\/\/(?P<creator>[^\.]+)\.fanbox\.cc\/', flags=re.IGNORECASE)

response = requests.get(f'https://www.pixiv.net/fanbox/creator/{PIXIV_ID}', allow_redirects=False)
creator_url = response.headers['Location']
creator_id = CREATOR_URL_REGEXP.match(creator_url).group('creator')


session = requests.Session()
session.headers.update({
    'Origin': 'https://www.fanbox.cc',
    'Referer': 'https://www.fanbox.cc',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36',
})
cookie = requests.cookies.create_cookie(name='FANBOXSESSID', value=FANBOX_COOKIE)
session.cookies.set_cookie(cookie)

plans = session.get(f'https://api.fanbox.cc/plan.listCreator?creatorId={creator_id}')
planData = json.loads(plans.text)

supporters = session.get('https://api.fanbox.cc/relationship.listFans?status=all')
supporterData = json.loads(supporters.text)

params = {'token': FANSYNC_TOKEN}
# don't use the same session here
requests.post(f'https://fansync.moe/api/creator/{PIXIV_ID}/plans', params=params, json=planData)
requests.post(f'https://fansync.moe/api/creator/{PIXIV_ID}/supporters', params=params, json=supporterData)
```
