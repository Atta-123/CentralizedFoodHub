"""Run against a LOCAL development API: python tests/api_workflow.py [http://localhost:5180].
Creates test users/restaurants/orders; do not run against production.
Uses only the Python standard library.
"""
import json, sys, uuid, urllib.request, urllib.error
BASE=(sys.argv[1] if len(sys.argv)>1 else 'http://localhost:5180').rstrip('/')
checks=0

def call(method,path,data=None,token=None,key=None,expected=200):
    global checks
    headers={'Content-Type':'application/json'}
    if token: headers['Authorization']='Bearer '+token
    if key: headers['Idempotency-Key']=key
    req=urllib.request.Request(BASE+path,data=json.dumps(data).encode() if data is not None else None,headers=headers,method=method)
    try:
        with urllib.request.urlopen(req,timeout=30) as response: status=response.status; body=response.read()
    except urllib.error.HTTPError as error: status=error.code; body=error.read()
    assert status==expected, f'{method} {path}: expected {expected}, got {status}: {body.decode()}'
    checks+=1
    return json.loads(body) if body else None

def login(email): return call('POST','/api/auth/login',{'email':email,'password':'Demo@12345'})['token']
admin=login('admin@foodhub.local'); customer=login('customer@foodhub.local'); abc=login('abc@foodhub.local'); pizza=login('pizza@foodhub.local')
call('GET','/api/admin/summary',token=customer,expected=403)
call('GET','/api/orders',expected=401)
call('POST','/api/auth/login',{'email':'admin@foodhub.local','password':'wrong'},expected=401)
restaurants=call('GET','/api/restaurants',token=admin)['items']
abc_r=next(r for r in restaurants if r['name']=='ABC Burgers')
pizza_r=next(r for r in restaurants if r['name']=='Peshawar Pizza')
owner_list=call('GET','/api/restaurants',token=abc)['items']
assert len(owner_list)==1 and owner_list[0]['id']==abc_r['id']
call('GET',f"/api/restaurants/{pizza_r['id']}/menu",token=abc,expected=403)
call('POST',f"/api/restaurants/{pizza_r['id']}/menu",{'name':'Unauthorized','description':'test','price':50,'isAvailable':True},token=abc,expected=403)
tag=uuid.uuid4().hex[:10]
new=call('POST','/api/restaurants',{'name':'Test Kitchen '+tag,'cuisine':'Test cuisine','address':'Peshawar test address','commissionPercent':20,'ownerEmail':tag+'@test.local','ownerPassword':'Demo@12345','latitude':34.015,'longitude':71.525,'deliveryRadiusKm':5},token=admin)
owner=login(tag+'@test.local')
item=call('POST',f"/api/restaurants/{new['id']}/menu",{'name':'Test Burger','description':'Commission test','price':500,'isAvailable':True},token=owner)
other_item=call('GET',f"/api/restaurants/{pizza_r['id']}/menu",token=customer)[0]
payload={'restaurantId':new['id'],'deliveryLatitude':34.015,'deliveryLongitude':71.525,'address':'Test street Peshawar','phone':'03001234567','items':[{'menuItemId':item['id'],'quantity':2}]}
call('POST','/api/orders',payload,token=customer,expected=400)
invalid={**payload,'items':[{'menuItemId':other_item['id'],'quantity':1}]}
call('POST','/api/orders',invalid,token=customer,key=uuid.uuid4().hex,expected=400)
call('POST','/api/orders',{**payload,'items':[{'menuItemId':item['id'],'quantity':0}]},token=customer,key=uuid.uuid4().hex,expected=400)
key=uuid.uuid4().hex
order=call('POST','/api/orders',payload,token=customer,key=key)
assert (order['subtotal'],order['commission'],order['restaurantPayable'])==(1000,200,800)
retry=call('POST','/api/orders',payload,token=customer,key=key);assert retry['id']==order['id']
call('POST','/api/orders',{**payload,'phone':'03009999999'},token=customer,key=key,expected=409)
call('PATCH',f"/api/orders/{order['id']}/status",{'status':'Accepted','version':order['version']},token=pizza,expected=404)
call('PATCH',f"/api/orders/{order['id']}/status",{'status':'Delivered','version':order['version']},token=owner,expected=403)
call('PATCH',f"/api/orders/{order['id']}/status",{'status':'Delivered','version':order['version']},token=admin,expected=409)
call('PATCH',f"/api/restaurants/{new['id']}",{'commissionPercent':30,'isOpen':True},token=admin)
call('PUT',f"/api/restaurants/{new['id']}/menu/{item['id']}",{'name':'Renamed Burger','description':'Changed price','price':700,'isAvailable':True},token=owner)
before=call('GET','/api/admin/summary',token=admin)
stale=order['version']
for status in ('Accepted','Preparing','ReadyForPickup'):
    order=call('PATCH',f"/api/orders/{order['id']}/status",{'status':status,'version':order['version']},token=owner)
call('PATCH',f"/api/orders/{order['id']}/status",{'status':'Delivered','version':stale},token=admin,expected=409)
order=call('PATCH',f"/api/orders/{order['id']}/status",{'status':'Delivered','version':order['version']},token=admin)
assert order['commissionPercent']==20 and order['commission']==200 and order['items'][0]['name']=='Test Burger' and order['items'][0]['unitPrice']==500
summary=call('GET','/api/admin/summary',token=admin)
assert summary['deliveredSales']==before['deliveredSales']+1000
assert summary['commissionRevenue']==before['commissionRevenue']+200
assert summary['restaurantPayable']==before['restaurantPayable']+800
settlements=call('GET','/api/settlements',token=owner)
assert len(settlements)==1 and settlements[0]['payable']==800
second=call('POST','/api/orders',payload,token=customer,key=uuid.uuid4().hex)
assert second['subtotal']==1400 and second['commission']==420
second=call('PATCH',f"/api/orders/{second['id']}/status",{'status':'Cancelled','version':second['version']},token=customer)
assert second['status']=='Cancelled'
from concurrent.futures import ThreadPoolExecutor
concurrent_key=uuid.uuid4().hex
with ThreadPoolExecutor(max_workers=2) as pool:
    concurrent=list(pool.map(lambda _:call('POST','/api/orders',payload,token=customer,key=concurrent_key),range(2)))
assert concurrent[0]['id']==concurrent[1]['id'], 'Simultaneous retries created duplicate orders'
call('PATCH',f"/api/restaurants/{new['id']}",{'commissionPercent':30,'isOpen':False},token=admin)
call('POST','/api/orders',payload,token=customer,key=uuid.uuid4().hex,expected=400)
rows=call('GET','/api/orders',token=owner)['items'];assert all(o['restaurantId']==new['id'] for o in rows)
new_customer=call('POST','/api/auth/register',{'name':'Test Customer','email':tag+'customer@test.local','password':'Demo@12345'})['token']
assert call('GET','/api/orders',token=new_customer)['totalCount']==0
print(f'PASS: {checks} HTTP checks plus commission, ownership, snapshot and settlement assertions.')
