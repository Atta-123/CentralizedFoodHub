"""Local development only. Run after starting API. Creates isolated fixture restaurants."""
from api_workflow import call, login, BASE
import uuid
admin=login('admin@foodhub.local');customer=login('customer@foodhub.local')
tag=uuid.uuid4().hex[:8]
def restaurant(name,lat,lng,radius):
    return call('POST','/api/restaurants',{'name':name+' '+tag,'cuisine':'NearbyTest'+tag,'address':'Test location','commissionPercent':20,'ownerEmail':name+tag+'@test.local','ownerPassword':'Demo@12345','latitude':lat,'longitude':lng,'deliveryRadiusKm':radius},token=admin)
a=restaurant('Nearest',34.015,71.525,5)
b=restaurant('Further',34.025,71.525,5)
c=restaurant('OutOfRange',34.035,71.525,0.1)
d=restaurant('FarAway',35.0,71.525,50)
query=f'/api/restaurants/nearby?latitude=34.015&longitude=71.525&radiusKm=5&search=NearbyTest{tag}'
rows=call('GET',query)['items']
assert [r['id'] for r in rows]==[a['id'],b['id']]
assert rows[0]['distanceKm']==0 and 1<rows[1]['distanceKm']<2
first=call('GET',query+'&pageSize=1&page=1');second=call('GET',query+'&pageSize=1&page=2')
assert first['totalCount']==2 and first['items'][0]['id']==a['id'] and second['items'][0]['id']==b['id']
assert call('GET',query.replace('radiusKm=5','radiusKm=0.5'))['totalCount']==1
assert call('GET',query.replace('search=NearbyTest'+tag,'search=Nearest%20'+tag))['totalCount']==1
assert call('GET',query.replace('latitude=34.015','latitude=-34.015'))['totalCount']==0
for bad in ['/api/restaurants/nearby','/api/restaurants/nearby?latitude=91&longitude=71','/api/restaurants/nearby?latitude=34&longitude=181','/api/restaurants/nearby?latitude=34&longitude=71&radiusKm=51']:
    call('GET',bad,expected=400)
item=call('POST',f"/api/restaurants/{a['id']}/menu",{'name':'Nearby meal','description':'Test','price':200,'isAvailable':True},token=admin)
payload={'restaurantId':a['id'],'address':'Test address','phone':'03001234567','items':[{'menuItemId':item['id'],'quantity':1}],'deliveryLatitude':34.015,'deliveryLongitude':71.525}
o=call('POST','/api/orders',payload,token=customer,key=uuid.uuid4().hex)
assert o['deliveryLatitude']==34.015 and o['deliveryLongitude']==71.525
call('POST','/api/orders',{**payload,'deliveryLatitude':35.0},token=customer,key=uuid.uuid4().hex,expected=400)
call('POST','/api/orders',{**payload,'deliveryLatitude':None},token=customer,key=uuid.uuid4().hex,expected=400)
call('PATCH',f"/api/restaurants/{a['id']}",{'commissionPercent':20,'isOpen':False},token=admin)
assert [r['id'] for r in call('GET',query)['items']]==[b['id']]
print('PASS: nearby sorting, distance, range, pagination, cuisine/name search, coordinates validation, closed filtering and checkout location enforcement.')
